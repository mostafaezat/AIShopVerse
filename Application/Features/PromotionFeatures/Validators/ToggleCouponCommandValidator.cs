using FluentValidation;

namespace Application.Features.PromotionFeatures.Commands
{
    public class ToggleCouponCommandValidator : AbstractValidator<ToggleCouponCommand>
    {
        public ToggleCouponCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("Id is required.");
        }
    }
}
