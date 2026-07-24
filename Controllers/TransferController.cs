using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OPA_Pay.Data;
using OPA_Pay.Helpers;
using OPA_Pay.Repositories.Interfaces;
using OPA_Pay.Services;
using System.Text.Json;
using OPA_Pay.ViewModels;
using System.Security.Claims;

namespace OPA_Pay.Controllers
{
    [Authorize]
    public class TransferController : Controller
    {
        private readonly ITransferRepository _repo;
        private readonly IAccountRepository _accounts;
        private readonly IBeneficiaryRepository _beneficiaries;
        private readonly ITransferService _transferService;
        private readonly IRecipientLookupService _lookup;
        private readonly ApplicationDbContext _context;

        public TransferController(
            ITransferRepository repo,
            IAccountRepository accounts,
            IBeneficiaryRepository beneficiaries,
            ITransferService transferService,
            IRecipientLookupService lookup,
            ApplicationDbContext context)
        {
            _repo = repo;
            _accounts = accounts;
            _beneficiaries = beneficiaries;
            _transferService = transferService;
            _lookup = lookup;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> EstimateFee(int accountId, decimal amount)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var estimate = await _transferService.EstimateFeeAsync(accountId, amount, userId);
            if (estimate == null)
                return Json(new { error = "Invalid account or amount." });

            return Json(new
            {
                estimate.Amount,
                estimate.Fee,
                estimate.Total,
                currency = estimate.CurrencyCode,
                fixedFeeUsd = estimate.FixedFeeUsd,
                percentage = estimate.Percentage
            });
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            return View(await _repo.GetByUserIdAsync(userId));
        }

        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var item = await _repo.GetByIdAsync(id);

            if (item == null || item.Account.UserId != userId)
                return NotFound();

            return View(item);
        }

        public async Task<IActionResult> Create()
        {
            await LoadTransferFormDataAsync();
            return View(new TransferCreateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TransferCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await LoadTransferFormDataAsync();
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _transferService.ExecuteTransferAsync(
                model.AccountId, model.BeneficiaryId, model.Amount, userId);

            if (!result.Success)
            {
                ModelState.AddModelError("", result.ErrorMessage ?? "Transfer failed.");
                await LoadTransferFormDataAsync();
                return View(model);
            }

            TempData["Success"] = "Transfer completed successfully.";
            return RedirectToAction(nameof(Details), new { id = result.TransferId });
        }

        public async Task<IActionResult> Mobile()
        {
            await LoadAccountSelectListAsync();
            await LoadMobileRecipientsAsync();
            return View(new MobileTransferViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Mobile(MobileTransferViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await LoadAccountSelectListAsync();
                await LoadMobileRecipientsAsync();
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _transferService.ExecuteMobileTransferAsync(
                model.AccountId, model.RecipientName, model.MobileNumber, model.Amount, userId);

            if (!result.Success)
            {
                ModelState.AddModelError("", result.ErrorMessage ?? "Transfer failed.");
                await LoadAccountSelectListAsync();
                await LoadMobileRecipientsAsync();
                return View(model);
            }

            var transfer = await _context.Transfers
                .Include(t => t.Account)
                    .ThenInclude(a => a.Currency)
                .FirstOrDefaultAsync(t => t.Id == result.TransferId);

            var code = transfer?.Account?.Currency?.Code ?? "";
            TempData["Success"] = transfer?.PickupCode != null
                ? $"Mobile transfer ready. Pickup code: {transfer.PickupCode}. Recipient collects {transfer.Amount:N2} {code} at any approved agent."
                : "Mobile transfer initiated. Recipient can pick up at any agent.";

            return RedirectToAction(nameof(Details), new { id = result.TransferId });
        }

        private async Task LoadTransferFormDataAsync()
        {
            await LoadAccountSelectListAsync();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var beneficiaries = (await _beneficiaries.GetByUserIdAsync(userId)).ToList();

            ViewBag.Beneficiaries = new SelectList(beneficiaries, "Id", "FullName");
            ViewBag.HasBeneficiaries = beneficiaries.Any();
        }

        private async Task LoadAccountSelectListAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var accounts = (await _accounts.GetByUserIdAsync(userId)).ToList();
            var activeId = HttpContext.Session.GetInt32("ActiveAccountId");

            ViewBag.Accounts = AccountSelectHelper.ToSelectList(accounts, activeId);
            ViewBag.NoAccounts = !AccountSelectHelper.HasAccounts(accounts);
        }

        private async Task LoadMobileRecipientsAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var recipients = await _lookup.GetMobileRecipientsAsync(userId);
            ViewBag.MobileRecipientsJson = JsonSerializer.Serialize(recipients,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            var saved = (await _beneficiaries.GetByUserIdAsync(userId))
                .Where(b => !string.IsNullOrWhiteSpace(b.MobileNumber))
                .OrderBy(b => b.FullName)
                .ToList();

            ViewBag.SavedRecipients = new SelectList(saved, "Id", "FullName");
            ViewBag.HasSavedRecipients = saved.Any();
        }
    }
}
