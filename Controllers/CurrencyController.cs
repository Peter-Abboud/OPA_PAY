using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OPA_Pay.Models;
using OPA_Pay.Repositories.Interfaces;

namespace OPA_Pay.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CurrencyController : Controller
    {
        private readonly ICurrencyRepository _repo;

        public CurrencyController(ICurrencyRepository repo)
        {
            _repo = repo;
        }

        public async Task<IActionResult> Index()
            => View(await _repo.GetAllAsync());

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Currency model)
        {
            if (!ModelState.IsValid) return View(model);

            await _repo.AddAsync(model);
            await _repo.SaveAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var item = await _repo.GetByIdAsync(id);
            return item == null ? NotFound() : View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Currency model)
        {
            if (!ModelState.IsValid) return View(model);

            await _repo.UpdateAsync(model);
            await _repo.SaveAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
