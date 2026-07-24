using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OPA_Pay.Repositories.Interfaces;

namespace OPA_Pay.Controllers
{
    [Authorize]
    public class MapController : Controller
    {
        private readonly IAgentProfileRepository _repo;

        public MapController(IAgentProfileRepository repo)
        {
            _repo = repo;
        }

        public async Task<IActionResult> Index()
        {
            var agents = await _repo.GetAllAsync();

            var mapItems = agents
                .Where(a => !string.IsNullOrWhiteSpace(a.OfficeName))
                .Select(a => new AgentMapItem
                {
                    Id = a.Id,
                    OfficeName = a.OfficeName,
                    City = a.City,
                    Latitude = NormalizeCoord(a.Latitude, 33.8938),
                    Longitude = NormalizeCoord(a.Longitude, 35.5018),
                    IsOpen = a.IsOpen,
                    IsApproved = a.IsApproved,
                    OpeningTime = a.OpeningTime.ToString(@"hh\:mm"),
                    ClosingTime = a.ClosingTime.ToString(@"hh\:mm")
                })
                .ToList();

            return View(mapItems);
        }

        private static double NormalizeCoord(double value, double fallback)
            => value == 0 ? fallback : value;

        public class AgentMapItem
        {
            public int Id { get; set; }
            public string OfficeName { get; set; } = "";
            public string City { get; set; } = "";
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public bool IsOpen { get; set; }
            public bool IsApproved { get; set; }
            public string OpeningTime { get; set; } = "";
            public string ClosingTime { get; set; } = "";
        }
    }
}
