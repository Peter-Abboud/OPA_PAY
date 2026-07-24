namespace OPA_Pay.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public string Title { get; set; }=string.Empty;

        public string Message { get; set; } = string.Empty;

        public bool IsRead { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string UserId { get; set; } = string.Empty;

        public ApplicationUser User { get; set; } = null!;
    }
}