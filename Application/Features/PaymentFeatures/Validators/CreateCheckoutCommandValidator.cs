using FluentValidation;

namespace Application.Features.PaymentFeatures.Commands
{
    public class CreateCheckoutCommandValidator : AbstractValidator<CreateCheckoutCommand>
    {
        public CreateCheckoutCommandValidator()
        {
            RuleFor(x => x.ShippingAddress).NotEmpty().MaximumLength(500).WithMessage("Shipping address is required (max 500).");
            RuleFor(x => x.CouponCode).MaximumLength(30).WithMessage("Coupon code must not exceed 30 characters.")
                .When(x => !string.IsNullOrEmpty(x.CouponCode));
        }
    }
}
