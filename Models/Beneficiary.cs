namespace OPA_Pay.Models
{
    public class Beneficiary
    {
        public int Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string AccountNumber { get; set; } = string.Empty;

        public string MobileNumber { get; set; } = string.Empty;

        public string BankName { get; set; } = string.Empty;

        public string Country { get; set; } = string.Empty;

        public string UserId { get; set; } = string.Empty;

        public ApplicationUser User { get; set; } = null!;

        public ICollection<Transfer> Transfers { get; set; }
            = new List<Transfer>();
    }
}