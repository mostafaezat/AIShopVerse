using FluentValidation;

namespace Application.Features.PromotionFeatures.Commands
{
    public class AddCouponCommandValidator : AbstractValidator<AddCouponCommand>
    {
        public AddCouponCommandValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Coupon code is required.")
                .MaximumLength(30).WithMessage("Coupon code must not exceed 30 characters.")
                .Matches("^[A-Za-z0-9]+$").WithMessage("Coupon code must contain only letters and digits.");

            RuleFor(x => x.DiscountValue)
                .GreaterThan(0).WithMessage("Discount value must be greater than zero.");

            RuleFor(x => x.MaxUses)
                .GreaterThanOrEqualTo(0).WithMessage("Max uses cannot be negative.");

            RuleFor(x => x.ValidTo)
                .GreaterThan(x => x.ValidFrom).WithMessage("Valid To must be after Valid From.");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description must not exceed 500 characters.")
                .When(x => !string.IsNullOrEmpty(x.Description));
        }
    }
}
