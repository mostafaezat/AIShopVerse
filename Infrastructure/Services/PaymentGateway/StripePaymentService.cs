using Microsoft.Extensions.Options;
using Stripe;

namespace Infrastructure.Services.PaymentGateway
{
    public class StripePaymentService : IStripePaymentService
    {
        private readonly StripeOptions _options;
        private readonly StripeClient? _client;

        public StripePaymentService(IOptions<StripeOptions> options)
        {
            _options = options.Value;
            _client = _options.IsEnabled ? new StripeClient(_options.SecretKey) : null;
        }

        public bool IsEnabled => _options.IsEnabled;

        public async Task<StripePaymentIntent> CreatePaymentIntentAsync(
            long amountInCents,
            string orderId,
            string orderNumber,
            CancellationToken cancellationToken = default)
        {
            var client = _client ?? throw new InvalidOperationException("Stripe is not configured.");
            var service = new PaymentIntentService(client);
            var intent = await service.CreateAsync(new PaymentIntentCreateOptions
            {
                Amount = amountInCents,
                Currency = "usd",
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true
                },
                Metadata = new Dictionary<string, string>
                {
                    ["order_id"] = orderId,
                    ["order_number"] = orderNumber
                }
            }, null, cancellationToken);

            return new StripePaymentIntent
            {
                PaymentIntentId = intent.Id,
                ClientSecret = intent.ClientSecret
            };
        }

        public async Task<bool> IsPaymentSucceededAsync(string paymentIntentId, CancellationToken cancellationToken = default)
        {
            var client = _client ?? throw new InvalidOperationException("Stripe is not configured.");
            var service = new PaymentIntentService(client);
            var intent = await service.GetAsync(paymentIntentId, null, null, cancellationToken);
            return intent?.Status == "succeeded";
        }

        public async Task<string?> RefundAsync(string paymentIntentId, long? amountInCents = null, CancellationToken cancellationToken = default)
        {
            var client = _client ?? throw new InvalidOperationException("Stripe is not configured.");
            var service = new RefundService(client);
            var refund = await service.CreateAsync(new RefundCreateOptions
            {
                PaymentIntent = paymentIntentId,
                Amount = amountInCents
            }, null, cancellationToken);
            return refund?.Id;
        }

        public StripeWebhookEvent? ReadWebhook(string json, string signature)
        {
            if (string.IsNullOrEmpty(_options.WebhookSecret))
                return null;

            Event stripeEvent;
            try
            {
                stripeEvent = EventUtility.ConstructEvent(json, signature, _options.WebhookSecret, throwOnApiVersionMismatch: false);
            }
            catch (StripeException)
            {
                return null;
            }

            var paymentIntent = stripeEvent.Data?.Object as PaymentIntent;
            return new StripeWebhookEvent
            {
                Type = stripeEvent.Type ?? string.Empty,
                PaymentIntentId = paymentIntent?.Id ?? string.Empty,
                Status = paymentIntent?.Status ?? string.Empty,
                AmountReceived = paymentIntent?.AmountReceived
            };
        }
    }
}
