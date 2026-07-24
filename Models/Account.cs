namespace OPA_Pay.Models
{
    public class Account
    {
        public int Id { get; set; }

        public string AccountNumber { get; set; }
            = Guid.NewGuid().ToString();

        public decimal Balance { get; set; }

        public string UserId { get; set; }
            = string.Empty;

        public int CurrencyId { get; set; }

        // NAVIGATION

        public ApplicationUser User { get; set; }
            = null!;

        public Currency Currency { get; set; }
            = null!;

        public ICollection<Transaction> Transactions { get; set; }
            = new List<Transaction>();

        public ICollection<Transfer> Transfers { get; set; }
            = new List<Transfer>();
    }
}