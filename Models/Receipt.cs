namespace OPA_Pay.Models
{
    public class Receipt
    {
        public int Id { get; set; }

        public string ReceiptNumber { get; set; } = Guid.NewGuid().ToString();

        public int TransferId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public Transfer Transfer { get; set; } = null!;
    }
}