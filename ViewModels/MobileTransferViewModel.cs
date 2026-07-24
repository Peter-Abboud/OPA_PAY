using System.ComponentModel.DataAnnotations;

namespace OPA_Pay.ViewModels
{
    public class MobileTransferViewModel
    {
        [Required]
        [Display(Name = "From Account")]
        public int AccountId { get; set; }

        [Required]
        [Display(Name = "Recipient Name")]
        public string RecipientName { get; set; } = string.Empty;

        [Required]
        [Phone]
        [Display(Name = "Mobile Number")]
        public string MobileNumber { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }
    }
}
