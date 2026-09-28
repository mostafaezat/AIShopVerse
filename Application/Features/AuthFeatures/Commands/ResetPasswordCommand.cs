using Application.DTO.AuthDtos;
using Microsoft.AspNetCore.Identity;

namespace Application.Features.AuthFeatures.Commands
{
    public class ResetPasswordCommand : IRequest<Result<ResetPasswordResponseDto>>
    {
        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;

        public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result<ResetPasswordResponseDto>>
        {
            private readonly UserManager<ApplicationUser> _userManager;

            public ResetPasswordCommandHandler(UserManager<ApplicationUser> userManager)
            {
                _userManager = userManager;
            }

            public async Task<Result<ResetPasswordResponseDto>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
            {
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    return Result<ResetPasswordResponseDto>.Falid(null, "Invalid request.");
                }

                var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return Result<ResetPasswordResponseDto>.Falid(null, errors);
                }

                return Result<ResetPasswordResponseDto>.Success(new ResetPasswordResponseDto
                {
                    Success = true,
                    Message = "Password has been reset successfully."
                }, "Password reset successful.");
            }
        }
    }

    public class ResetPasswordResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
