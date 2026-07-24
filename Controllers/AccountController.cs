using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OPA_Pay.Data;
using OPA_Pay.Helpers;
using OPA_Pay.Models;
using OPA_Pay.Repositories.Interfaces;
using OPA_Pay.Services;
using static OPA_Pay.Services.WalletSetupService;
using OPA_Pay.ViewModels;
using System.Security.Claims;

namespace OPA_Pay.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly IAccountRepository _repo;
        private readonly ICurrencyRepository _currencies;
        private readonly ICurrencyConversionService _conversion;
        private readonly ApplicationDbContext _context;

        public AccountController(
            IAccountRepository repo,
            ICurrencyRepository currencies,
            ICurrencyConversionService conversion,
            ApplicationDbContext context)
        {
            _repo = repo;
            _currencies = currencies;
            _conversion = conversion;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var accounts = (await _repo.GetByUserIdAsync(userId)).ToList();

            if (User.IsInRole("Client"))
            {
                var ids = accounts.Select(a => a.CurrencyId).ToHashSet();
                ViewBag.CanCreateWallet = !DefaultCurrencyIds.All(id => ids.Contains(id));
            }
            else
            {
                ViewBag.CanCreateWallet = true;
            }

            return View(accounts);
        }

        public async Task<IActionResult> Details(int id)
        {
            var item = await GetOwnedAccountAsync(id);
            return item == null ? NotFound() : View(item);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (User.IsInRole("Client"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                var currencyIds = await _context.Accounts
                    .Where(a => a.UserId == userId)
                    .Select(a => a.CurrencyId)
                    .ToListAsync();

                if (DefaultCurrencyIds.All(id => currencyIds.Contains(id)))
                {
                    TempData["Info"] = "You already have USD, EUR, and LBP wallets. Use Add Funds to request a deposit.";
                    return RedirectToAction(nameof(Index));
                }
            }

            await LoadCurrenciesAsync();
            return View(new AccountCreateViewModel { Balance = 0, CurrencyId = 1 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AccountCreateViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            if (!ModelState.IsValid)
            {
                await LoadCurrenciesAsync(model.CurrencyId);
                return View(model);
            }

            var alreadyHasCurrency = await _context.Accounts
                .AnyAsync(a => a.UserId == userId && a.CurrencyId == model.CurrencyId);

            if (alreadyHasCurrency)
            {
                var currency = await _currencies.GetByIdAsync(model.CurrencyId);
                ModelState.AddModelError(nameof(model.CurrencyId),
                    $"You already have a {currency?.Code ?? "wallet"} account. Pick a different currency.");
                await LoadCurrenciesAsync(model.CurrencyId);
                return View(model);
            }

            var account = new Account
            {
                UserId = userId,
                CurrencyId = model.CurrencyId,
                Balance = model.Balance,
                AccountNumber = SerialNumberGenerator.AccountNumber()
            };

            await _repo.AddAsync(account);
            await _repo.SaveAsync();

            HttpContext.Session.SetInt32("ActiveAccountId", account.Id);

            var currencyCode = (await _currencies.GetByIdAsync(model.CurrencyId))?.Code ?? "";
            TempData["Success"] = $"Wallet created: {currencyCode} with balance {model.Balance:N2}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SwitchActive(int accountId)
        {
            var account = await GetOwnedAccountAsync(accountId);
            if (account == null)
                return NotFound();

            HttpContext.Session.SetInt32("ActiveAccountId", accountId);
            TempData["Success"] = $"Active wallet: {account.Currency?.Code} ({account.Balance:N2})";

            var referer = Request.Headers.Referer.ToString();
            if (!string.IsNullOrEmpty(referer) && Uri.TryCreate(referer, UriKind.Absolute, out _))
                return Redirect(referer);

            return RedirectToAction("Index", "Dashboard");
        }

        [HttpGet]
        public async Task<IActionResult> Exchange()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var accounts = (await _repo.GetByUserIdAsync(userId)).ToList();

            ViewBag.FromAccounts = AccountSelectHelper.ToSelectList(accounts);
            ViewBag.ToAccounts = AccountSelectHelper.ToSelectList(accounts);
            ViewBag.HasAccounts = accounts.Count >= 2;

            return View(new CurrencyExchangeViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Exchange(CurrencyExchangeViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var accounts = (await _repo.GetByUserIdAsync(userId)).ToList();

            if (model.FromAccountId == model.ToAccountId)
                ModelState.AddModelError("", "Select two different accounts.");

            var from = accounts.FirstOrDefault(a => a.Id == model.FromAccountId);
            var to = accounts.FirstOrDefault(a => a.Id == model.ToAccountId);

            if (from == null || to == null)
                ModelState.AddModelError("", "Invalid account selection.");

            if (from != null && from.Balance < model.Amount)
                ModelState.AddModelError("", "Insufficient balance. Overdraft is not available.");

            if (!ModelState.IsValid)
            {
                ViewBag.FromAccounts = AccountSelectHelper.ToSelectList(accounts);
                ViewBag.ToAccounts = AccountSelectHelper.ToSelectList(accounts);
                ViewBag.HasAccounts = accounts.Count >= 2;
                return View(model);
            }

            var converted = await _conversion.ConvertBetweenCurrenciesAsync(
                model.Amount, from!.CurrencyId, to!.CurrencyId);

            from.Balance -= model.Amount;
            to.Balance += converted;

            await _repo.UpdateAsync(from);
            await _repo.UpdateAsync(to);
            await _repo.SaveAsync();

            TempData["Success"] = $"Converted {model.Amount:N2} {from.Currency?.Code} → {converted:N2} {to.Currency?.Code}";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var item = await GetOwnedAccountAsync(id);
            if (item == null) return NotFound();

            await LoadCurrenciesAsync(item.CurrencyId);
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Account model)
        {
            var existing = await GetOwnedAccountAsync(model.Id);
            if (existing == null) return NotFound();

            ModelStateHelper.RemoveNavigationProperties(ModelState);

            if (!ModelState.IsValid)
            {
                await LoadCurrenciesAsync(model.CurrencyId);
                return View(model);
            }

            existing.CurrencyId = model.CurrencyId;
            await _repo.UpdateAsync(existing);
            await _repo.SaveAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await GetOwnedAccountAsync(id);
            return item == null ? NotFound() : View(item);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await GetOwnedAccountAsync(id);
            if (item == null) return NotFound();

            await _repo.DeleteAsync(id);
            await _repo.SaveAsync();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var remaining = (await _repo.GetByUserIdAsync(userId)).FirstOrDefault();
            if (remaining != null)
                HttpContext.Session.SetInt32("ActiveAccountId", remaining.Id);
            else
                HttpContext.Session.Remove("ActiveAccountId");

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadCurrenciesAsync(int? selectedId = null)
        {
            var list = await _currencies.GetAllAsync();
            ViewBag.Currencies = new SelectList(list, "Id", "Code", selectedId);
        }

        private async Task<Account?> GetOwnedAccountAsync(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var item = await _repo.GetByIdAsync(id);
            return item != null && item.UserId == userId ? item : null;
        }
    }
}
