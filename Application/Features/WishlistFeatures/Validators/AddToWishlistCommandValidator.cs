using FluentValidation;

namespace Application.Features.WishlistFeatures.Commands
{
    public class AddToWishlistCommandValidator : AbstractValidator<AddToWishlistCommand>
    {
        public AddToWishlistCommandValidator()
        {
            RuleFor(x => x.ProductId).NotEmpty().WithMessage("Product id is required.");
        }
    }
}
