using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace OPA_Pay.Helpers
{
    public static class ModelStateHelper
    {
        public static void RemoveNavigationProperties(ModelStateDictionary modelState)
        {
            var keys = modelState.Keys
                .Where(k => k.Contains("User") || k.Contains("Currency") ||
                            k.Contains("Transactions") || k.Contains("Transfers") ||
                            k.Contains("Beneficiary") || k.Contains("Account") ||
                            k.Contains("AgentProfile") || k.Contains("Receipt"))
                .ToList();

            foreach (var key in keys)
                modelState.Remove(key);
        }
    }
}
