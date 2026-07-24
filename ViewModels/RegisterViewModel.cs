using System.ComponentModel.DataAnnotations;

namespace OPA_Pay.ViewModels
{
    public class RegisterViewModel
    {
        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Compare("Password")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required]
        public string RoleType { get; set; } = string.Empty;

        [Display(Name = "Preferred Currency")]
        [Range(1, 3, ErrorMessage = "Select USD, EUR, or LBP.")]
        public int PreferredCurrencyId { get; set; } = 1;
    }
}