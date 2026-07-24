using System.ComponentModel.DataAnnotations;

namespace OPA_Pay.ViewModels
{
    public class BeneficiaryCreateViewModel
    {
        [Required(ErrorMessage = "Full name is required.")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "OPA Account Number")]
        public string? AccountNumber { get; set; }

        [Display(Name = "Mobile Number (optional)")]
        public string? MobileNumber { get; set; }

        [Display(Name = "Bank Name")]
        public string? BankName { get; set; }

        [Display(Name = "Country")]
        public string? Country { get; set; }
    }
}
