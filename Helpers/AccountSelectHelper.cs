using Microsoft.AspNetCore.Mvc.Rendering;
using OPA_Pay.Models;

namespace OPA_Pay.Helpers
{
    public static class AccountSelectHelper
    {
        public static SelectList ToSelectList(IEnumerable<Account> accounts, int? selectedId = null)
        {
            var items = accounts.Select(a => new SelectListItem
            {
                Value = a.Id.ToString(),
                Text = $"{a.AccountNumber} — {a.Currency?.Code ?? "N/A"} {a.Balance:N2}",
                Selected = selectedId.HasValue && a.Id == selectedId.Value
            }).ToList();

            return new SelectList(items, "Value", "Text", selectedId?.ToString());
        }

        public static bool HasAccounts(IEnumerable<Account> accounts)
            => accounts.Any();
    }
}
