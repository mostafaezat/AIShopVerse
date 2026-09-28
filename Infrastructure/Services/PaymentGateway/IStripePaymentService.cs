namespace Infrastructure.Services.PaymentGateway
{
    public interface IStripePaymentService
    {
        bool IsEnabled { get; }

        Task<StripePaymentIntent> CreatePaymentIntentAsync(
            long amountInCents,
            string orderId,
            string orderNumber,
            CancellationToken cancellationToken = default);

        Task<bool> IsPaymentSucceededAsync(string paymentIntentId, CancellationToken cancellationToken = default);

        Task<string?> RefundAsync(string paymentIntentId, long? amountInCents = null, CancellationToken cancellationToken = default);

        StripeWebhookEvent? ReadWebhook(string json, string signature);
    }
}
