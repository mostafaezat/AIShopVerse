using FluentValidation;

namespace Application.Features.NotificationFeatures.Commands
{
    public class CreateNotificationCommandValidator : AbstractValidator<CreateNotificationCommand>
    {
        public CreateNotificationCommandValidator()
        {
            RuleFor(x => x.UserId).NotEmpty().WithMessage("User id is required.");
            RuleFor(x => x.Title).NotEmpty().MaximumLength(200).WithMessage("Title is required (max 200).");
            RuleFor(x => x.Type).NotEmpty().WithMessage("Notification type is required.");
        }
    }
}
