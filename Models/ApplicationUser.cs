using Microsoft.AspNetCore.Identity;

namespace OPA_Pay.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;

        public string RoleType { get; set; } = string.Empty;

        /// <summary>Default wallet currency: 1=USD, 2=EUR, 3=LBP</summary>
        public int PreferredCurrencyId { get; set; } = 1;

        public DateTime CreatedAt { get; set; }
            = DateTime.Now;

        // NAVIGATION

        public ICollection<Account> Accounts { get; set; }
            = new List<Account>();

        public ICollection<Beneficiary> Beneficiaries { get; set; }
            = new List<Beneficiary>();

        public ICollection<Notification> Notifications { get; set; }
            = new List<Notification>();

        public ICollection<Review> Reviews { get; set; }
            = new List<Review>();

        public Agent? AgentProfile { get; set; }
    }
}