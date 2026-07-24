namespace OPA_Pay.Models
{
    public class Currency
    {
        public int Id { get; set; }

        public string Code { get; set; }
            = string.Empty;

        public string Name { get; set; }
            = string.Empty;

        public decimal ExchangeRate { get; set; }

        public ICollection<Account> Accounts { get; set; }
            = new List<Account>();
    }
}