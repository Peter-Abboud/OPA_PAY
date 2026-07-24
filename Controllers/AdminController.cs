using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OPA_Pay.Repositories.Interfaces;
using OPA_Pay.Services;
using OPA_Pay.ViewModels;
using System.Security.Claims;

namespace OPA_Pay.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IAccountRepository _accounts;
        private readonly ITransferRepository _transfers;
        private readonly ITransactionRepository _transactions;
        private readonly IAgentProfileRepository _agents;
        private readonly IReviewRepository _reviews;
        private readonly IFundRequestService _fundRequests;

        public AdminController(
            IAccountRepository accounts,
            ITransferRepository transfers,
            ITransactionRepository transactions,
            IAgentProfileRepository agents,
            IReviewRepository reviews,
            IFundRequestService fundRequests)
        {
            _accounts = accounts;
            _transfers = transfers;
            _transactions = transactions;
            _agents = agents;
            _reviews = reviews;
            _fundRequests = fundRequests;
        }

        public async Task<IActionResult> Index()
        {
            var accounts = await _accounts.GetAllAsync();
            var transfers = await _transfers.GetAllAsync();
            var transactions = await _transactions.GetAllAsync();
            var agents = await _agents.GetAllAsync();
            var reviews = await _reviews.GetAllAsync();
            var fundOverview = await _fundRequests.GetAllForAdminAsync();

            ViewBag.TotalAccounts = accounts.Count();
            ViewBag.TotalTransfers = transfers.Count();
            ViewBag.TotalTransactions = transactions.Count();
            ViewBag.PendingAgents = agents.Count(a => !a.IsApproved);
            ViewBag.PendingFundRequests = fundOverview.PendingCount;
            ViewBag.TotalFundRequests = fundOverview.TotalCount;
            ViewBag.AverageRating = reviews.Any() ? reviews.Average(r => r.Rating).ToString("N1") : "N/A";
            ViewBag.TotalTransferVolume = transfers.Sum(t => t.Amount);
            ViewBag.LatestTransactions = transactions.OrderByDescending(x => x.CreatedAt).Take(10).ToList();

            return View();
        }

        public async Task<IActionResult> FundRequests(string? status)
        {
            return View(await _fundRequests.GetAllForAdminAsync(status));
        }

        public async Task<IActionResult> Agents()
        {
            return View(await _agents.GetAllAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveAgent(int id)
        {
            var agent = await _agents.GetByIdAsync(id);
            if (agent == null) return NotFound();

            agent.IsApproved = true;
            if (agent.Latitude == 0) agent.Latitude = 33.8938;
            if (agent.Longitude == 0) agent.Longitude = 35.5018;
            await _agents.UpdateAsync(agent);
            await _agents.SaveAsync();

            TempData["Success"] = $"Agent {agent.OfficeName} approved and added to the map.";
            return RedirectToAction(nameof(Agents));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectAgent(int id)
        {
            var agent = await _agents.GetByIdAsync(id);
            if (agent == null) return NotFound();

            agent.IsApproved = false;
            await _agents.UpdateAsync(agent);
            await _agents.SaveAsync();

            TempData["Success"] = $"Agent {agent.OfficeName} rejected.";
            return RedirectToAction(nameof(Agents));
        }
    }
}
