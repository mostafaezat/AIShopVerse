namespace Infrastructure.Services.PaymentGateway
{
    public class PaymentOptions
    {
        public int PendingOrderTimeoutMinutes { get; set; } = 30;

        public int OrderExpiryIntervalMinutes { get; set; } = 1;
    }
}
