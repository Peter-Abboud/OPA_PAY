using System.ComponentModel.DataAnnotations;

namespace OPA_Pay.ViewModels
{
    public class CurrencyExchangeViewModel
    {
        [Required]
        [Display(Name = "From Account")]
        public int FromAccountId { get; set; }

        [Required]
        [Display(Name = "To Account")]
        public int ToAccountId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }
    }
}
