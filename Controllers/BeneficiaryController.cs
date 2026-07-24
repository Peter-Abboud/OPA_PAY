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
    [Authorize]
    public class BeneficiaryController : Controller
    {
        private readonly IBeneficiaryRepository _repo;
        private readonly IRecipientLookupService _lookup;
        private readonly ApplicationDbContext _context;

        public BeneficiaryController(
            IBeneficiaryRepository repo,
            IRecipientLookupService lookup,
            ApplicationDbContext context)
        {
            _repo = repo;
            _lookup = lookup;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            return View(await _repo.GetByUserIdAsync(userId));
        }

        public async Task<IActionResult> Details(int id)
        {
            var item = await GetOwnedBeneficiaryAsync(id);
            return item == null ? NotFound() : View(item);
        }

        public IActionResult Create() => View(new BeneficiaryCreateViewModel());

        [HttpGet]
        public async Task<IActionResult> Lookup(string term)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var results = await _lookup.SearchAsync(term ?? "", userId);
            return Json(results);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BeneficiaryCreateViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.MobileNumber))
                model.MobileNumber = null;

            if (!ModelState.IsValid)
                return View(model);

            var beneficiary = new Beneficiary
            {
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!,
                FullName = model.FullName.Trim(),
                AccountNumber = model.AccountNumber?.Trim() ?? "",
                MobileNumber = model.MobileNumber?.Trim() ?? "",
                BankName = model.BankName?.Trim() ?? "",
                Country = model.Country?.Trim() ?? ""
            };

            await _repo.AddAsync(beneficiary);
            await _repo.SaveAsync();

            TempData["Success"] = "Beneficiary saved successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var item = await GetOwnedBeneficiaryAsync(id);
            if (item == null) return NotFound();

            return View(new BeneficiaryEditViewModel
            {
                Id = item.Id,
                FullName = item.FullName,
                AccountNumber = item.AccountNumber,
                MobileNumber = string.IsNullOrEmpty(item.MobileNumber) ? null : item.MobileNumber,
                BankName = item.BankName,
                Country = item.Country
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(BeneficiaryEditViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.MobileNumber))
                model.MobileNumber = null;

            var existing = await GetOwnedBeneficiaryAsync(model.Id);
            if (existing == null) return NotFound();

            if (!ModelState.IsValid)
                return View(model);

            existing.FullName = model.FullName.Trim();
            existing.AccountNumber = model.AccountNumber?.Trim() ?? "";
            existing.MobileNumber = model.MobileNumber?.Trim() ?? "";
            existing.BankName = model.BankName?.Trim() ?? "";
            existing.Country = model.Country?.Trim() ?? "";

            await _repo.UpdateAsync(existing);
            await _repo.SaveAsync();

            TempData["Success"] = "Beneficiary updated.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var item = await GetOwnedBeneficiaryAsync(id);
            return item == null ? NotFound() : View(item);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await GetOwnedBeneficiaryAsync(id);
            if (item == null) return NotFound();

            if (await _context.Transfers.AnyAsync(t => t.BeneficiaryId == id))
            {
                TempData["Error"] = "Cannot delete: this beneficiary is linked to past transfers.";
                return RedirectToAction(nameof(Edit), new { id });
            }

            await _repo.DeleteAsync(id);
            await _repo.SaveAsync();

            TempData["Success"] = "Beneficiary deleted.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<Beneficiary?> GetOwnedBeneficiaryAsync(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var item = await _repo.GetByIdAsync(id);
            return item != null && item.UserId == userId ? item : null;
        }
    }
}
