namespace OPA_Pay.Services
{
    public class TransferResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int? TransferId { get; set; }
    }

    public class FeeEstimate
    {
        public decimal Amount { get; set; }
        public decimal Fee { get; set; }
        public decimal Total { get; set; }
        public string CurrencyCode { get; set; } = "USD";
        public decimal FixedFeeUsd { get; set; }
        public decimal Percentage { get; set; }
    }

    public interface ITransferService
    {
        Task<TransferResult> ExecuteTransferAsync(int accountId, int beneficiaryId, decimal amount, string userId);
        Task<TransferResult> ExecuteMobileTransferAsync(int accountId, string recipientName, string mobileNumber, decimal amount, string userId);
        Task<FeeEstimate?> EstimateFeeAsync(int accountId, decimal amount, string userId);
    }
}
