using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OPA_Pay.Data;
using OPA_Pay.Helpers;
using OPA_Pay.Repositories.Interfaces;
using OPA_Pay.Services;
using System.Security.Claims;

namespace OPA_Pay.Controllers
{
    [Authorize]
    public class StripeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAccountRepository _accounts;
        private readonly IFundRequestService _fundRequests;

        public StripeController(
            ApplicationDbContext context,
            IAccountRepository accounts,
            IFundRequestService fundRequests)
        {
            _context = context;
            _accounts = accounts;
            _fundRequests = fundRequests;
        }

        [HttpGet]
        public async Task<IActionResult> Deposit()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var accounts = (await _accounts.GetByUserIdAsync(userId)).ToList();
            var activeId = HttpContext.Session.GetInt32("ActiveAccountId");

            ViewBag.Accounts = AccountSelectHelper.ToSelectList(accounts, activeId);
            ViewBag.NoAccounts = !AccountSelectHelper.HasAccounts(accounts);

            var pending = await _context.FundRequests
                .Include(f => f.Account)
                    .ThenInclude(a => a.Currency)
                .Where(f => f.UserId == userId && f.Status == "Pending")
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            ViewBag.PendingRequests = pending;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestFunds(int accountId, decimal amount)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var (success, error) = await _fundRequests.SubmitRequestAsync(userId, accountId, amount);

            if (!success)
            {
                TempData["Error"] = error;
                return RedirectToAction(nameof(Deposit));
            }

            TempData["Success"] = "Fund request submitted. Partner agents were notified. Funds will be added after an agent approves your request.";
            return RedirectToAction(nameof(Deposit));
        }
    }
}
