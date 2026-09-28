using FluentValidation;

namespace Application.Features.ReviewFeatures.Commands
{
    public class ApproveReviewCommandValidator : AbstractValidator<ApproveReviewCommand>
    {
        public ApproveReviewCommandValidator()
        {
            RuleFor(x => x.ReviewId).NotEmpty().WithMessage("Review id is required.");
        }
    }
}
