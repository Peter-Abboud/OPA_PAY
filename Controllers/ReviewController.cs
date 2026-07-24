using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using Microsoft.EntityFrameworkCore;

using OPA_Pay.Data;

using OPA_Pay.Helpers;

using OPA_Pay.Models;

using OPA_Pay.Repositories.Interfaces;

using OPA_Pay.Services;

using OPA_Pay.ViewModels;

using System.Security.Claims;



namespace OPA_Pay.Controllers

{

    [Authorize]

    public class ReviewController : Controller

    {

        private readonly IReviewRepository _reviews;

        private readonly IAgentProfileRepository _agents;

        private readonly ApplicationDbContext _context;

        private readonly IEmailService _email;



        public ReviewController(

            IReviewRepository reviews,

            IAgentProfileRepository agents,

            ApplicationDbContext context,

            IEmailService email)

        {

            _reviews = reviews;

            _agents = agents;

            _context = context;

            _email = email;

        }



        public async Task<IActionResult> Index()

        {

            var approved = (await _agents.GetAllAsync()).Where(a => a.IsApproved).ToList();



            var stats = await _context.Reviews

                .GroupBy(r => r.AgentProfileId)

                .Select(g => new

                {

                    AgentId = g.Key,

                    Average = g.Average(r => r.Rating),

                    Count = g.Count()

                })

                .ToListAsync();



            var statsMap = stats.ToDictionary(s => s.AgentId);



            var model = approved

                .Select(a => new AgentReviewCardViewModel

                {

                    Agent = a,

                    AverageRating = statsMap.TryGetValue(a.Id, out var s) ? s.Average : 0,

                    ReviewCount = statsMap.TryGetValue(a.Id, out var s2) ? s2.Count : 0

                })

                .OrderByDescending(x => x.ReviewCount)

                .ThenByDescending(x => x.AverageRating)

                .ToList();



            ViewBag.PendingCount = (await _agents.GetAllAsync()).Count(a => !a.IsApproved);

            ViewBag.EmailEnabled = _email.IsEnabled;



            return View(model);

        }



        public async Task<IActionResult> Create(int agentId)

        {

            var agent = await _agents.GetByIdAsync(agentId);

            if (agent == null || !agent.IsApproved)

            {

                TempData["Error"] = "This agent is not available for reviews.";

                return RedirectToAction(nameof(Index));

            }



            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var ownAgent = await _agents.GetByUserIdAsync(userId);

            if (ownAgent != null && ownAgent.Id == agentId)

            {

                TempData["Error"] = "You cannot review your own agent office.";

                return RedirectToAction(nameof(Index));

            }



            ViewBag.AgentName = agent.OfficeName;

            return View(new Review { AgentProfileId = agentId, Rating = 5 });

        }



        [HttpPost]

        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Create(Review model)

        {

            ModelStateHelper.RemoveNavigationProperties(ModelState);



            if (model.Rating < 1 || model.Rating > 5)

                ModelState.AddModelError(nameof(model.Rating), "Rating must be between 1 and 5.");



            if (string.IsNullOrWhiteSpace(model.Comment))

                ModelState.AddModelError(nameof(model.Comment), "Please enter a comment.");



            var agent = await _agents.GetByIdAsync(model.AgentProfileId);

            if (agent == null || !agent.IsApproved)

                ModelState.AddModelError("", "Invalid agent.");



            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var ownAgent = await _agents.GetByUserIdAsync(userId);

            if (ownAgent != null && ownAgent.Id == model.AgentProfileId)

                ModelState.AddModelError("", "You cannot review your own agent office.");



            if (!ModelState.IsValid)

            {

                ViewBag.AgentName = agent?.OfficeName;

                return View(model);

            }



            model.UserId = userId;



            await _reviews.AddAsync(model);

            await _reviews.SaveAsync();



            if (agent != null)

            {

                await _email.SendToUserAsync(agent.UserId, "OPA Pay — New review received",

                    $"<div style='font-family:Segoe UI,sans-serif'><h2 style='color:#2563eb'>OPA Pay</h2><p>Your office <strong>{agent.OfficeName}</strong> received a <strong>{model.Rating}/5</strong> review.</p></div>");

            }



            TempData["Success"] = "Thank you for your review!";

            return RedirectToAction(nameof(Index));

        }



        public async Task<IActionResult> AgentReviews(int id)

        {

            var agent = await _agents.GetByIdAsync(id);

            if (agent == null) return NotFound();



            var reviews = await _reviews.GetByAgentIdAsync(id);

            ViewBag.Agent = agent;

            ViewBag.AverageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;

            ViewBag.ReviewCount = reviews.Count;



            return View(reviews);

        }

    }

}


