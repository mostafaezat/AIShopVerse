namespace Infrastructure.Services.PaymentGateway
{
    public class StripeOptions
    {
        public string SecretKey { get; set; } = string.Empty;
        public string PublishableKey { get; set; } = string.Empty;
        public string WebhookSecret { get; set; } = string.Empty;

        public bool IsEnabled => !string.IsNullOrEmpty(SecretKey) && !string.IsNullOrEmpty(PublishableKey);
    }

    public class StripePaymentIntent
    {
        public string PaymentIntentId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
    }

    public class StripeWebhookEvent
    {
        public string Type { get; set; } = string.Empty;
        public string PaymentIntentId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public long? AmountReceived { get; set; }
    }
}
