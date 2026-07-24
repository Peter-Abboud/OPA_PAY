using System.ComponentModel.DataAnnotations;

namespace OPA_Pay.ViewModels
{
    public class BeneficiaryEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Full name is required.")]
        public string FullName { get; set; } = string.Empty;

        public string? AccountNumber { get; set; }

        public string? MobileNumber { get; set; }

        public string? BankName { get; set; }

        public string? Country { get; set; }
    }
}
