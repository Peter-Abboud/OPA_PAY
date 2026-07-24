using OPA_Pay.Models;

namespace OPA_Pay.ViewModels
{
    public class AgentReviewCardViewModel
    {
        public Agent Agent { get; set; } = null!;
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }

        public string AverageDisplay => ReviewCount == 0 ? "No reviews yet" : $"{AverageRating:0.0} / 5";
    }
}
