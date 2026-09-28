using FluentValidation;

namespace Application.Features.CartFeatures.Commands
{
    public class RemoveCartItemCommandValidator : AbstractValidator<RemoveCartItemCommand>
    {
        public RemoveCartItemCommandValidator()
        {
            RuleFor(x => x.CartItemId).NotEmpty().WithMessage("Cart item id is required.");
        }
    }
}
