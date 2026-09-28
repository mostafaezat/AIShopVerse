using FluentValidation;

namespace Application.Features.OrderFeatures.Commands
{
    public class CancelOrderCommandValidator : AbstractValidator<CancelOrderCommand>
    {
        public CancelOrderCommandValidator()
        {
            RuleFor(x => x.OrderId).NotEmpty().WithMessage("Order id is required.");
        }
    }
}
