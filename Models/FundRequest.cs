namespace OPA_Pay.Models
{
    public class FundRequest
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public int AccountId { get; set; }

        public decimal Amount { get; set; }

        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? ProcessedAt { get; set; }

        public string? ProcessedByUserId { get; set; }

        public ApplicationUser User { get; set; } = null!;

        public Account Account { get; set; } = null!;
    }
}
