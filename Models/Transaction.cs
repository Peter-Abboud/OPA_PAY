namespace OPA_Pay.Models
{
    public class Transaction
    {
        public int Id { get; set; }

        public decimal Amount { get; set; }

        public string Type { get; set; } = "Transfer";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int AccountId { get; set; }

        public int? TransferId { get; set; }

        public Account Account { get; set; } = null!;
        public Transfer? Transfer { get; set; }
    }
}