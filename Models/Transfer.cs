namespace OPA_Pay.Models
{
    public class Transfer
    {
        public int Id { get; set; }

        public string Reference { get; set; }
            = Guid.NewGuid().ToString();

        public decimal Amount { get; set; }

        public decimal Fee { get; set; }

        public string Status { get; set; }
            = "Pending";

        public string TransferMethod { get; set; } = "Beneficiary";

        /// <summary>6-digit code for mobile cash pickup at an agent office.</summary>
        public string? PickupCode { get; set; }

        public DateTime CreatedAt { get; set; }
            = DateTime.Now;

        // FK

        public int AccountId { get; set; }

        public int BeneficiaryId { get; set; }

        // NAVIGATION

        public Account Account { get; set; }
            = null!;

        public Beneficiary Beneficiary { get; set; }
            = null!;

        public Transaction? Transaction { get; set; }

        public Receipt? Receipt { get; set; }
    }
}