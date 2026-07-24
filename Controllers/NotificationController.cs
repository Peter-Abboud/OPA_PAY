using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OPA_Pay.Models;
using OPA_Pay.Repositories.Interfaces;
using System.Security.Claims;

namespace OPA_Pay.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly INotificationRepository _repo;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationController(
            INotificationRepository repo,
            UserManager<ApplicationUser> userManager)
        {
            _repo = repo;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User)!;
            return View(await _repo.GetByUserIdAsync(userId));
        }

        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            var notif = await _repo.GetByIdAsync(id);

            if (notif == null || notif.UserId != userId)
                return NotFound();

            notif.IsRead = true;
            await _repo.SaveAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> MarkAllRead()
        {
            var userId = _userManager.GetUserId(User)!;
            var notifications = await _repo.GetByUserIdAsync(userId);

            foreach (var n in notifications.Where(n => !n.IsRead))
                n.IsRead = true;

            await _repo.SaveAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
