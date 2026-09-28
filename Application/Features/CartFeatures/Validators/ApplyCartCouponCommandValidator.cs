using FluentValidation;

namespace Application.Features.CartFeatures.Commands
{
    public class ApplyCartCouponCommandValidator : AbstractValidator<ApplyCartCouponCommand>
    {
        public ApplyCartCouponCommandValidator()
        {
            RuleFor(x => x.CouponCode).NotEmpty().MaximumLength(30).WithMessage("Coupon code is required (max 30).");
        }
    }
}
