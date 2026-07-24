using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using MimeKit;
using OPA_Pay.Configuration;
using OPA_Pay.Models;

namespace OPA_Pay.Services
{
    public interface IEmailService
    {
        bool IsEnabled { get; }
        Task<bool> SendAsync(string toEmail, string subject, string htmlBody);
        Task SendToUserAsync(string userId, string subject, string htmlBody);
        Task SendToRoleAsync(string role, string subject, string htmlBody);
    }

    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            IOptions<EmailSettings> settings,
            UserManager<ApplicationUser> userManager,
            ILogger<EmailService> logger)
        {
            _settings = settings.Value;
            _userManager = userManager;
            _logger = logger;
        }

        public bool IsEnabled => _settings.IsConfigured;

        public async Task<bool> SendAsync(string toEmail, string subject, string htmlBody)
        {
            if (!IsEnabled || string.IsNullOrWhiteSpace(toEmail))
                return false;

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
                message.To.Add(MailboxAddress.Parse(toEmail));
                message.Subject = subject;
                message.Body = new TextPart("html") { Text = htmlBody };

                using var client = new SmtpClient();
                await client.ConnectAsync(_settings.SmtpServer, _settings.Port, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_settings.Username, _settings.Password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send email to {Email}: {Subject}", toEmail, subject);
                return false;
            }
        }

        public async Task SendToUserAsync(string userId, string subject, string htmlBody)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user?.Email != null)
                await SendAsync(user.Email, subject, htmlBody);
        }

        public async Task SendToRoleAsync(string role, string subject, string htmlBody)
        {
            var users = await _userManager.GetUsersInRoleAsync(role);
            foreach (var user in users)
            {
                if (!string.IsNullOrWhiteSpace(user.Email))
                    await SendAsync(user.Email, subject, htmlBody);
            }
        }
    }
}
