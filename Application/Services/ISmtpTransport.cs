namespace Application.Services
{
    public interface ISmtpTransport
    {
        Task SendAsync(EmailMessage message, EmailOptions options, CancellationToken cancellationToken = default);
    }
}