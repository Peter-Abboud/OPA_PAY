using System.ComponentModel.DataAnnotations;

namespace OPA_Pay.ViewModels
{
    public class AgentProfileViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Office name is required.")]
        public string OfficeName { get; set; } = string.Empty;

        [Required(ErrorMessage = "City is required.")]
        public string City { get; set; } = string.Empty;

        public double Latitude { get; set; } = 33.8938;

        public double Longitude { get; set; } = 35.5018;

        public bool LocationConfirmed { get; set; }

        [Required]
        [Display(Name = "Opening Time")]
        public string OpeningTime { get; set; } = "08:00";

        [Required]
        [Display(Name = "Closing Time")]
        public string ClosingTime { get; set; } = "18:00";

        public bool IsApproved { get; set; }
    }
}
