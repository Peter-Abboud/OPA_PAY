using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OPA_Pay.Repositories.Interfaces;
using System.Security.Claims;

namespace OPA_Pay.Controllers
{
    [Authorize]
    public class ReceiptController : Controller
    {
        private readonly IReceiptRepository _receipts;
        private readonly ITransferRepository _transfers;

        public ReceiptController(IReceiptRepository receipts, ITransferRepository transfers)
        {
            _receipts = receipts;
            _transfers = transfers;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            return View(await _receipts.GetByUserIdAsync(userId));
        }

        public async Task<IActionResult> Details(int transferId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var transfer = await _transfers.GetByIdAsync(transferId);

            if (transfer == null || transfer.Account.UserId != userId)
                return NotFound();

            var receipt = await _receipts.GetByTransferIdAsync(transferId);
            if (receipt == null) return NotFound();

            ViewBag.Transfer = transfer;
            return View(receipt);
        }
    }
}
