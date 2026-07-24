using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OPA_Pay.Data;
using OPA_Pay.Models;
using OPA_Pay.Repositories.Interfaces;
using OPA_Pay.Services;
using OPA_Pay.ViewModels;
using System.Security.Claims;

namespace OPA_Pay.Controllers
{
    [Authorize(Roles = "Agent")]
    public class AgentController : Controller
    {
        private readonly ITransactionRepository _transactions;
        private readonly IAgentProfileRepository _agents;
        private readonly ApplicationDbContext _context;
        private readonly IFundRequestService _fundRequests;
        private readonly IEmailService _email;

        public AgentController(
            ITransactionRepository transactions,
            IAgentProfileRepository agents,
            ApplicationDbContext context,
            IFundRequestService fundRequests,
            IEmailService email)
        {
            _transactions = transactions;
            _agents = agents;
            _context = context;
            _fundRequests = fundRequests;
            _email = email;
        }

        public async Task<IActionResult> FundRequests()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var profile = await _agents.GetByUserIdAsync(userId);
            ViewBag.IsApproved = profile?.IsApproved ?? false;
            ViewBag.OfficeName = profile?.OfficeName;

            return View(await _fundRequests.GetPendingAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveFundRequest(int id)
        {
            if (!await CanProcessFundRequestsAsync())
                return RedirectToAction(nameof(FundRequests));

            var agentId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            if (await _fundRequests.ApproveAsync(id, agentId))
                TempData["Success"] = "Fund request approved. Client wallet credited.";
            else
                TempData["Error"] = "Could not approve request.";

            return RedirectToAction(nameof(FundRequests));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectFundRequest(int id)
        {
            if (!await CanProcessFundRequestsAsync())
                return RedirectToAction(nameof(FundRequests));

            var agentId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            if (await _fundRequests.RejectAsync(id, agentId))
                TempData["Success"] = "Fund request rejected.";
            else
                TempData["Error"] = "Could not reject request.";

            return RedirectToAction(nameof(FundRequests));
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var profile = await _agents.GetByUserIdAsync(userId);

            ViewBag.Profile = profile;
            ViewBag.IsApproved = profile?.IsApproved ?? false;

            var mobileTransfers = await _context.Transfers
                .Include(t => t.Beneficiary)
                .Where(t => t.TransferMethod == "Mobile" && t.Status == "Pending Pickup")
                .OrderByDescending(t => t.CreatedAt)
                .Take(10)
                .ToListAsync();

            ViewBag.PendingPickups = mobileTransfers.Count;
            ViewBag.RecentPickups = mobileTransfers;

            var commissionRate = 0.02m;
            ViewBag.TotalCommissions = mobileTransfers.Sum(t => t.Amount * commissionRate);

            return View(profile);
        }

        public async Task<IActionResult> Profile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var profile = await _agents.GetByUserIdAsync(userId);

            if (profile == null)
                return View(new AgentProfileViewModel());

            var vm = MapToViewModel(profile);
            vm.LocationConfirmed = profile.Latitude != 0 && profile.Longitude != 0;
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(AgentProfileViewModel model)
        {
            if (!model.LocationConfirmed || model.Latitude == 0 || model.Longitude == 0)
                ModelState.AddModelError("", "Place your branch on the map and confirm the location before saving.");

            if (!ModelState.IsValid)
                return View(model);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var existing = await _agents.GetByUserIdAsync(userId);

            if (!TimeSpan.TryParse(model.OpeningTime, out var openTime))
                openTime = new TimeSpan(8, 0, 0);

            if (!TimeSpan.TryParse(model.ClosingTime, out var closeTime))
                closeTime = new TimeSpan(18, 0, 0);

            var lat = model.Latitude;
            var lng = model.Longitude;

            if (existing == null)
            {
                await _agents.AddAsync(new Agent
                {
                    UserId = userId,
                    OfficeName = model.OfficeName,
                    City = model.City,
                    Latitude = lat,
                    Longitude = lng,
                    OpeningTime = openTime,
                    ClosingTime = closeTime,
                    IsOpen = false,
                    IsApproved = false
                });
            }
            else
            {
                existing.OfficeName = model.OfficeName;
                existing.City = model.City;
                existing.Latitude = lat;
                existing.Longitude = lng;
                existing.OpeningTime = openTime;
                existing.ClosingTime = closeTime;
                await _agents.UpdateAsync(existing);
            }

            await _agents.SaveAsync();
            TempData["Success"] = "Profile saved successfully.";
            return RedirectToAction(nameof(Profile));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleOpen()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var profile = await _agents.GetByUserIdAsync(userId);

            if (profile == null) return NotFound();

            profile.IsOpen = !profile.IsOpen;
            await _agents.UpdateAsync(profile);
            await _agents.SaveAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> CashIn()
        {
            var pending = await _context.Transfers
                .Include(t => t.Beneficiary)
                .Include(t => t.Account)
                    .ThenInclude(a => a.Currency)
                .Where(t => t.TransferMethod == "Mobile" && t.Status == "Pending Pickup")
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return View(pending);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompletePickup(int transferId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var profile = await _agents.GetByUserIdAsync(userId);

            if (profile == null || !profile.IsApproved)
            {
                TempData["Error"] = "Your agent profile must be approved first.";
                return RedirectToAction(nameof(CashIn));
            }

            var transfer = await _context.Transfers
                .Include(t => t.Beneficiary)
                .Include(t => t.Account)
                    .ThenInclude(a => a.Currency)
                .FirstOrDefaultAsync(t => t.Id == transferId);

            if (transfer == null || transfer.Status != "Pending Pickup")
            {
                TempData["Error"] = "Transfer not found or already completed.";
                return RedirectToAction(nameof(CashIn));
            }

            if (transfer.Account == null)
            {
                TempData["Error"] = "Transfer account is missing.";
                return RedirectToAction(nameof(CashIn));
            }

            transfer.Status = "Completed";

            var commission = Math.Round(transfer.Amount * 0.02m, 2);
            var currency = transfer.Account.Currency?.Code ?? "";

            // Transfer already has one linked transaction (created at send time).
            // Commission is recorded separately without TransferId (one-to-one constraint).
            await _context.Transactions.AddAsync(new Transaction
            {
                AccountId = transfer.AccountId,
                TransferId = null,
                Amount = commission,
                Type = "Agent Commission",
                CreatedAt = DateTime.Now
            });

            var pickupInfo = string.IsNullOrEmpty(transfer.PickupCode)
                ? transfer.Reference
                : $"code {transfer.PickupCode}, ref {transfer.Reference}";

            await _context.Notifications.AddAsync(new Notification
            {
                UserId = transfer.Account.UserId,
                Title = "Pickup Completed",
                Message = $"{transfer.Beneficiary?.FullName} collected {transfer.Amount:N2} {currency} at {profile.OfficeName} ({pickupInfo}).",
                IsRead = false,
                CreatedAt = DateTime.Now
            });

            await _email.SendToUserAsync(transfer.Account.UserId, "OPA Pay — Cash pickup completed",
                $"<div style='font-family:Segoe UI,sans-serif'><h2 style='color:#2563eb'>OPA Pay</h2><p><strong>{transfer.Beneficiary?.FullName}</strong> collected <strong>{transfer.Amount:N2} {currency}</strong> at <strong>{profile.OfficeName}</strong> ({pickupInfo}).</p></div>");

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Pickup completed for {transfer.Beneficiary?.FullName} ({transfer.Amount:N2} {currency}).";
            return RedirectToAction(nameof(CashIn));
        }

        private async Task<bool> CanProcessFundRequestsAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var profile = await _agents.GetByUserIdAsync(userId);

            if (profile != null && profile.IsApproved)
                return true;

            TempData["Error"] = "Your agent profile must be approved before you can approve fund requests.";
            return false;
        }

        private static AgentProfileViewModel MapToViewModel(Agent agent) => new()
        {
            Id = agent.Id,
            OfficeName = agent.OfficeName,
            City = agent.City,
            Latitude = agent.Latitude,
            Longitude = agent.Longitude,
            OpeningTime = agent.OpeningTime.ToString(@"hh\:mm"),
            ClosingTime = agent.ClosingTime.ToString(@"hh\:mm"),
            IsApproved = agent.IsApproved,
            LocationConfirmed = agent.Latitude != 0 && agent.Longitude != 0
        };
    }
}
