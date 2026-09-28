using FluentValidation;

namespace Application.Features.OrderFeatures.Commands
{
    public class CheckoutCommandValidator : AbstractValidator<CheckoutCommand>
    {
        public CheckoutCommandValidator()
        {
            RuleFor(x => x.ShippingAddress)
                .NotEmpty().WithMessage("Shipping address is required.")
                .MaximumLength(500).WithMessage("Shipping address must not exceed 500 characters.");

            RuleFor(x => x.BillingAddress)
                .MaximumLength(500).WithMessage("Billing address must not exceed 500 characters.")
                .When(x => !string.IsNullOrEmpty(x.BillingAddress));
        }
    }
}
