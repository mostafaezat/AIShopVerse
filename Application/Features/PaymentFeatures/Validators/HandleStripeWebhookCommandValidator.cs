using FluentValidation;

namespace Application.Features.PaymentFeatures.Commands
{
    public class HandleStripeWebhookCommandValidator : AbstractValidator<HandleStripeWebhookCommand>
    {
        public HandleStripeWebhookCommandValidator()
        {
            RuleFor(x => x.Json).NotEmpty().WithMessage("Webhook payload is required.");
            RuleFor(x => x.Signature).NotEmpty().WithMessage("Webhook signature is required.");
        }
    }
}
