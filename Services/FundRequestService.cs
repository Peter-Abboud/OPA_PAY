using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OPA_Pay.Data;
using OPA_Pay.Models;
using OPA_Pay.ViewModels;

namespace OPA_Pay.Services
{
    public interface IFundRequestService
    {
        Task<(bool Success, string? Error)> SubmitRequestAsync(string userId, int accountId, decimal amount);
        Task<bool> ApproveAsync(int fundRequestId, string processedByUserId);
        Task<bool> RejectAsync(int fundRequestId, string processedByUserId);
        Task<List<FundRequest>> GetPendingAsync();
        Task<FundRequestAdminListViewModel> GetAllForAdminAsync(string? statusFilter = null);
    }

    public class FundRequestService : IFundRequestService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _email;

        public FundRequestService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IEmailService email)
        {
            _context = context;
            _userManager = userManager;
            _email = email;
        }

        public async Task<(bool Success, string? Error)> SubmitRequestAsync(string userId, int accountId, decimal amount)
        {
            if (amount <= 0)
                return (false, "Amount must be greater than zero.");

            var account = await _context.Accounts
                .Include(a => a.Currency)
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId);

            if (account == null)
                return (false, "Account not found.");

            var hasPending = await _context.FundRequests
                .AnyAsync(f => f.UserId == userId && f.AccountId == accountId && f.Status == "Pending");

            if (hasPending)
                return (false, "You already have a pending fund request for this wallet.");

            var request = new FundRequest
            {
                UserId = userId,
                AccountId = accountId,
                Amount = amount,
                Status = "Pending",
                CreatedAt = DateTime.Now
            };

            await _context.FundRequests.AddAsync(request);
            await _context.SaveChangesAsync();

            var clientName = account.User?.FullName ?? "A client";
            var currency = account.Currency?.Code ?? "";
            var msg = $"{clientName} requested to add {amount:N2} {currency} to wallet {account.AccountNumber}.";

            await NotifyUserAsync(userId, "Fund Request Submitted",
                $"Your request to add {amount:N2} {currency} is pending agent approval.");

            await NotifyRoleAsync("Agent", "New Fund Request — Action Required", msg);

            await _email.SendToUserAsync(userId, "OPA Pay — Fund request submitted",
                EmailBody($"Your request to add <strong>{amount:N2} {currency}</strong> is pending agent approval."));
            await _email.SendToRoleAsync("Agent", "OPA Pay — New fund request",
                EmailBody($"<strong>{clientName}</strong> requested <strong>{amount:N2} {currency}</strong>. Please review in Fund Requests."));

            return (true, null);
        }

        public async Task<bool> ApproveAsync(int fundRequestId, string processedByUserId)
        {
            var request = await _context.FundRequests
                .Include(f => f.Account)
                    .ThenInclude(a => a.Currency)
                .Include(f => f.User)
                .FirstOrDefaultAsync(f => f.Id == fundRequestId);

            if (request == null || request.Status != "Pending")
                return false;

            request.Status = "Approved";
            request.ProcessedAt = DateTime.Now;
            request.ProcessedByUserId = processedByUserId;

            request.Account.Balance += request.Amount;

            await _context.Transactions.AddAsync(new Transaction
            {
                AccountId = request.AccountId,
                Amount = request.Amount,
                Type = "Deposit",
                CreatedAt = DateTime.Now
            });

            var currency = request.Account.Currency?.Code ?? "";
            var approverLabel = await GetApproverLabelAsync(processedByUserId);
            await NotifyUserAsync(request.UserId, "Fund Request Approved",
                $"Your deposit was approved by {approverLabel}. {request.Amount:N2} {currency} was added to your wallet.");

            await _email.SendToUserAsync(request.UserId, "OPA Pay — Deposit approved",
                EmailBody($"Your deposit of <strong>{request.Amount:N2} {currency}</strong> was approved by {approverLabel} and credited to your wallet."));

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RejectAsync(int fundRequestId, string processedByUserId)
        {
            var request = await _context.FundRequests.FindAsync(fundRequestId);
            if (request == null || request.Status != "Pending")
                return false;

            request.Status = "Rejected";
            request.ProcessedAt = DateTime.Now;
            request.ProcessedByUserId = processedByUserId;

            var approverLabel = await GetApproverLabelAsync(processedByUserId);
            await NotifyUserAsync(request.UserId, "Fund Request Rejected",
                $"Your fund request was rejected by {approverLabel}. Contact support for details.");

            await _email.SendToUserAsync(request.UserId, "OPA Pay — Deposit request rejected",
                EmailBody($"Your fund request was rejected by {approverLabel}. Contact an agent for details."));

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<FundRequest>> GetPendingAsync()
        {
            return await _context.FundRequests
                .Include(f => f.User)
                .Include(f => f.Account)
                    .ThenInclude(a => a.Currency)
                .Where(f => f.Status == "Pending")
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
        }

        public async Task<FundRequestAdminListViewModel> GetAllForAdminAsync(string? statusFilter = null)
        {
            var query = _context.FundRequests
                .Include(f => f.User)
                .Include(f => f.Account)
                    .ThenInclude(a => a.Currency)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(statusFilter))
                query = query.Where(f => f.Status == statusFilter);

            var requests = await query
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            var all = await _context.FundRequests.ToListAsync();
            var rows = new List<FundRequestAdminRowViewModel>();

            foreach (var request in requests)
            {
                rows.Add(new FundRequestAdminRowViewModel
                {
                    Request = request,
                    ProcessedByDisplay = await GetProcessorDisplayAsync(request.ProcessedByUserId)
                });
            }

            return new FundRequestAdminListViewModel
            {
                Rows = rows,
                FilterStatus = statusFilter,
                TotalCount = all.Count,
                PendingCount = all.Count(f => f.Status == "Pending"),
                ApprovedCount = all.Count(f => f.Status == "Approved"),
                RejectedCount = all.Count(f => f.Status == "Rejected")
            };
        }

        private async Task<string?> GetProcessorDisplayAsync(string? userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return null;

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return "Unknown user";

            if (await _userManager.IsInRoleAsync(user, "Agent"))
            {
                var office = await _context.AgentProfiles
                    .Where(a => a.UserId == userId)
                    .Select(a => a.OfficeName)
                    .FirstOrDefaultAsync();

                return string.IsNullOrWhiteSpace(office)
                    ? $"{user.FullName} (Agent)"
                    : $"{office} — {user.FullName}";
            }

            if (await _userManager.IsInRoleAsync(user, "Admin"))
                return $"{user.FullName} (Admin)";

            return user.FullName;
        }

        private async Task NotifyUserAsync(string userId, string title, string message)
        {
            await _context.Notifications.AddAsync(new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                IsRead = false,
                CreatedAt = DateTime.Now
            });
        }

        private async Task NotifyRoleAsync(string role, string title, string message)
        {
            var users = await _userManager.GetUsersInRoleAsync(role);
            foreach (var user in users)
            {
                await NotifyUserAsync(user.Id, title, message);
            }
        }

        private async Task<string> GetApproverLabelAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return "OPA Pay";

            if (await _userManager.IsInRoleAsync(user, "Agent"))
            {
                var office = await _context.AgentProfiles
                    .Where(a => a.UserId == userId)
                    .Select(a => a.OfficeName)
                    .FirstOrDefaultAsync();

                return string.IsNullOrWhiteSpace(office)
                    ? user.FullName
                    : $"agent {office}";
            }

            if (await _userManager.IsInRoleAsync(user, "Admin"))
                return "OPA Pay admin";

            return user.FullName;
        }

        private static string EmailBody(string content)
            => $"<div style='font-family:Segoe UI,sans-serif'><h2 style='color:#2563eb'>OPA Pay</h2><p>{content}</p></div>";
    }
}
