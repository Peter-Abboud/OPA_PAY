namespace OPA_Pay.Models
{
    public class Review
    {
        public int Id { get; set; }

        public int Rating { get; set; }

        public string Comment { get; set; }
            = string.Empty;

        public string UserId { get; set; }
            = string.Empty;

        public int AgentProfileId { get; set; }

        // NAVIGATION

        public ApplicationUser User { get; set; }
            = null!;

        public Agent AgentProfile { get; set; }
            = null!;
    }
}