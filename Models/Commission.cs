namespace OPA_Pay.Models
{
    public class Commission
    {
        public int Id { get; set; }

        public decimal Percentage { get; set; }

        public decimal FixedAmount { get; set; }

        public bool IsActive { get; set; }
    }
}