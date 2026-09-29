using System.Net;
using System.Net.Mail;

namespace Application.Services
{
    public class SmtpClientTransport : ISmtpTransport
    {
        public async Task SendAsync(EmailMessage message, EmailOptions options, CancellationToken cancellationToken = default)
        {
            using var smtpClient = new SmtpClient
            {
                Host = options.SmtpHost,
                Port = options.SmtpPort,
                EnableSsl = options.UseSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 10000
            };

            if (!string.IsNullOrEmpty(options.Username))
            {
                smtpClient.UseDefaultCredentials = false;
                smtpClient.Credentials = new NetworkCredential(options.Username, options.Password);
            }

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(options.FromAddress, options.FromDisplayName),
                Subject = message.Subject,
                Body = message.Body,
                IsBodyHtml = message.IsHtml
            };
            mailMessage.To.Add(message.To);

            await smtpClient.SendMailAsync(mailMessage, cancellationToken).ConfigureAwait(false);
        }
    }
}