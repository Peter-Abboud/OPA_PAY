using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OPA_Pay.Models;
using OPA_Pay.Repositories.Interfaces;

namespace OPA_Pay.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CommissionController : Controller
    {
        private readonly ICommissionRepository _repo;

        public CommissionController(ICommissionRepository repo)
        {
            _repo = repo;
        }

        public async Task<IActionResult> Index()
            => View(await _repo.GetAllAsync());

        public IActionResult Edit(int id)
        {
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int id, decimal percentage, decimal fixedAmount)
        {
            var all = await _repo.GetAllAsync();
            var commission = all.FirstOrDefault(c => c.Id == id);

            if (commission == null) return NotFound();

            foreach (var c in all)
                c.IsActive = c.Id == id;

            commission.Percentage = percentage;
            commission.FixedAmount = fixedAmount;
            commission.IsActive = true;

            await _repo.UpdateAsync(commission);
            await _repo.SaveAsync();

            TempData["Success"] = "Commission structure updated.";
            return RedirectToAction(nameof(Index));
        }
    }
}
