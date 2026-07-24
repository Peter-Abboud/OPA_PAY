using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OPA_Pay.Repositories.Interfaces;
using System.Security.Claims;

namespace OPA_Pay.Controllers
{
    [Authorize]
    public class TransactionController : Controller
    {
        private readonly ITransactionRepository _repo;

        public TransactionController(ITransactionRepository repo)
        {
            _repo = repo;
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
    }
}
