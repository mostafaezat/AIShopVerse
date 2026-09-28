using Microsoft.Extensions.Logging;

namespace Application.Services
{
    public class DevEmailService : IEmailService
    {
        private readonly ILogger<DevEmailService> _logger;

        public DevEmailService(ILogger<DevEmailService> logger)
        {
            _logger = logger;
        }

        public Task SendPasswordResetEmailAsync(string toEmail, string resetLink)
        {
            _logger.LogWarning("[DEV EMAIL] Password reset link for {Email}: {Link}", toEmail, resetLink);
            return Task.CompletedTask;
        }
    }
}
