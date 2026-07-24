using System.ComponentModel.DataAnnotations;

namespace OPA_Pay.ViewModels
{
    public class AccountCreateViewModel
    {
        [Range(0, double.MaxValue, ErrorMessage = "Balance cannot be negative.")]
        public decimal Balance { get; set; }

        [Required(ErrorMessage = "Please select a currency.")]
        [Display(Name = "Currency")]
        public int CurrencyId { get; set; }
    }
}
