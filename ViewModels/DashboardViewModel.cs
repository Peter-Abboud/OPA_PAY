namespace OPA_Pay.ViewModels

{

    public class DashboardViewModel

    {

        public int AccountCount { get; set; }

        public int TransferCount { get; set; }

        public int UnreadNotifications { get; set; }

        public List<WalletBalanceSummary> WalletBalances { get; set; } = new();

        public List<MonthlyTransferStat> MonthlyTransfers { get; set; } = new();

    }



    public class WalletBalanceSummary

    {

        public string CurrencyCode { get; set; } = string.Empty;

        public string CurrencyName { get; set; } = string.Empty;

        public string AccountNumber { get; set; } = string.Empty;

        public decimal Balance { get; set; }

        public bool IsActive { get; set; }

    }



    public class MonthlyTransferStat

    {

        public string Month { get; set; } = string.Empty;

        public decimal Amount { get; set; }

    }

}


