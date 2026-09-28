using FluentValidation;

namespace Application.Features.BrandFeatures.Commands
{
    public class AddBrandCommandValidator : AbstractValidator<AddBrandCommand>
    {
        public AddBrandCommandValidator()
        {
            RuleFor(x => x.NameAR).NotEmpty().MaximumLength(100).WithMessage("Arabic name is required (max 100).");
            RuleFor(x => x.NameEN).NotEmpty().MaximumLength(100).WithMessage("English name is required (max 100).");
        }
    }
}
