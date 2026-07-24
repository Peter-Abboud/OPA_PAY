using OPA_Pay.Models;

namespace OPA_Pay.ViewModels
{
    public class FundRequestAdminRowViewModel
    {
        public FundRequest Request { get; set; } = null!;
        public string? ProcessedByDisplay { get; set; }
    }

    public class FundRequestAdminListViewModel
    {
        public List<FundRequestAdminRowViewModel> Rows { get; set; } = new();
        public string? FilterStatus { get; set; }
        public int TotalCount { get; set; }
        public int PendingCount { get; set; }
        public int ApprovedCount { get; set; }
        public int RejectedCount { get; set; }
    }
}
