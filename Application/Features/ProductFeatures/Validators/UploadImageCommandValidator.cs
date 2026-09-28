using FluentValidation;

namespace Application.Features.ProductFeatures.Commands
{
    public class UploadImageCommandValidator : AbstractValidator<UploadImageCommand>
    {
        public UploadImageCommandValidator()
        {
            RuleFor(x => x.File).NotNull().NotEmpty().WithMessage("A valid image file is required.");
        }
    }
}
