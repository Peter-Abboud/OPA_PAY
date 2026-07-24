using OPA_Pay.Models;

namespace OPA_Pay.ViewModels
{
    public class ActiveAccountSwitcherViewModel
    {
        public List<Account> Accounts { get; set; } = new();
        public int? ActiveAccountId { get; set; }

        public Account? ActiveAccount =>
            Accounts.FirstOrDefault(a => a.Id == ActiveAccountId);
    }
}
