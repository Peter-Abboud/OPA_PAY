namespace OPA_Pay.Models
{
    public class Agent
    {
        public int Id { get; set; }

        public string OfficeName { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;

        public double Latitude { get; set; } 

        public double Longitude { get; set; }

        public TimeSpan OpeningTime { get; set; }

        public TimeSpan ClosingTime { get; set; }

        public bool IsOpen { get; set; } = true;


        public bool IsApproved { get; set; }

        public string UserId { get; set; } = string.Empty;

        public ApplicationUser User { get; set; } = null!;
    }
}