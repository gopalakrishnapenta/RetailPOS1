using System.Net;
using System.Net.Mail;
using IdentityService.Interfaces;

namespace IdentityService.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            try
            {
                var smtpHost = _config["EmailSettings:Host"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(_config["EmailSettings:Port"] ?? "587");
                var fromEmail = _config["EmailSettings:UserEmail"];
                var appPassword = _config["EmailSettings:AppPassword"];

                if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(appPassword))
                {
                    _logger.LogWarning("Email sending is disabled. Please configure EmailSettings:UserEmail and EmailSettings:AppPassword in appsettings.json.");
                    return;
                }

                _logger.LogInformation($"[SMTP] Attempting to send email to {to} via {smtpHost}:{smtpPort}");

                using var client = new SmtpClient(smtpHost, smtpPort)
                {
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromEmail, appPassword),
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Timeout = 15000 // 15-second timeout
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail, "NexusPOS Support"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(to);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation($"[SMTP] Email sent successfully to {to}");
            }
            catch (SmtpException smtpEx)
            {
                _logger.LogError(smtpEx, $"[SMTP] Error sending to {to}: {smtpEx.Message} (StatusCode: {smtpEx.StatusCode})");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[SMTP] General error sending email to {to}");
                throw;
            }
        }
    }
}
