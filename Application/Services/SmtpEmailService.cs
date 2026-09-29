using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    public class SmtpEmailService : IEmailService
    {
        private readonly EmailOptions _options;
        private readonly ISmtpTransport _transport;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(IConfiguration configuration, ISmtpTransport transport, ILogger<SmtpEmailService> logger)
        {
            _options = configuration.GetSection("Email").Get<EmailOptions>() ?? new EmailOptions();
            _transport = transport;
            _logger = logger;
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string resetLink)
        {
            if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.SmtpHost) || string.IsNullOrWhiteSpace(_options.FromAddress))
            {
                _logger.LogWarning(
                    "SMTP email is not configured (Email:Enabled={Enabled}, Host={HasHost}, From={HasFrom}); falling back to dev log. Password reset link for {Email}: {Link}",
                    _options.Enabled,
                    !string.IsNullOrWhiteSpace(_options.SmtpHost),
                    !string.IsNullOrWhiteSpace(_options.FromAddress),
                    toEmail,
                    resetLink);
                return;
            }

            var message = new EmailMessage
            {
                To = toEmail,
                Subject = "AIShopVerse – Password Reset",
                Body = $"<p>Hello,</p><p>We received a request to reset your password.</p><p>Click the link below to choose a new one:</p><p><a href=\"{resetLink}\">{resetLink}</a></p><p>If you did not request this, you can safely ignore this email.</p>",
                IsHtml = true
            };

            try
            {
                await _transport.SendAsync(message, _options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to send password reset email to {Email} via SMTP host {Host}. The reset link remains valid; consider a manual resend.",
                    toEmail,
                    _options.SmtpHost);
            }
        }
    }
}