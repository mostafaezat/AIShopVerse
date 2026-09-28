using FluentValidation;

namespace Application.Features.PaymentFeatures.Commands
{
    public class CompletePaymentCommandValidator : AbstractValidator<CompletePaymentCommand>
    {
        public CompletePaymentCommandValidator()
        {
            RuleFor(x => x.OrderId).NotEmpty().WithMessage("Order id is required.");
            RuleFor(x => x.PaymentIntentId).NotEmpty().WithMessage("Payment intent id is required.");
        }
    }
}
