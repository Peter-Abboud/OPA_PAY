using System.ComponentModel.DataAnnotations;

namespace OPA_Pay.ViewModels
{
    public class TransferCreateViewModel
    {
        [Required]
        [Display(Name = "From Account")]
        public int AccountId { get; set; }

        [Required]
        [Display(Name = "Beneficiary")]
        public int BeneficiaryId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }
    }
}
