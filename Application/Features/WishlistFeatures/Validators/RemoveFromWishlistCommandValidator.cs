using FluentValidation;

namespace Application.Features.WishlistFeatures.Commands
{
    public class RemoveFromWishlistCommandValidator : AbstractValidator<RemoveFromWishlistCommand>
    {
        public RemoveFromWishlistCommandValidator()
        {
            RuleFor(x => x.ProductId).NotEmpty().WithMessage("Product id is required.");
        }
    }
}
