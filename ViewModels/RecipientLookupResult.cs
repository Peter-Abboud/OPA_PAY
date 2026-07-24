namespace OPA_Pay.ViewModels
{
    public class RecipientLookupResult
    {
        public string FullName { get; set; } = string.Empty;
        public List<string> OpaAccountNumbers { get; set; } = new();
        public List<string> BankNames { get; set; } = new();
        public List<string> Countries { get; set; } = new();
        public List<string> MobileNumbers { get; set; } = new();
        public string Source { get; set; } = string.Empty;
    }
}
