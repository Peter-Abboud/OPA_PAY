namespace OPA_Pay.Configuration
{
    public class EmailSettings
    {
        public string SmtpServer { get; set; } = "smtp.gmail.com";
        public int Port { get; set; } = 587;
        public string SenderName { get; set; } = "OPA Pay";
        public string SenderEmail { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(SmtpServer)
            && !string.IsNullOrWhiteSpace(SenderEmail)
            && !string.IsNullOrWhiteSpace(Username)
            && !string.IsNullOrWhiteSpace(Password)
            && !SenderEmail.Contains("your_email", StringComparison.OrdinalIgnoreCase)
            && !Password.Contains("your_app_password", StringComparison.OrdinalIgnoreCase);
    }
}
