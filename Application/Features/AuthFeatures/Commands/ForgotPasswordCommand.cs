using Application.DTO.AuthDtos;
using Microsoft.AspNetCore.Identity;

namespace Application.Features.AuthFeatures.Commands
{
    public class ForgotPasswordCommand : IRequest<Result<ForgotPasswordResponseDto>>
    {
        public string Email { get; set; } = string.Empty;

        public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result<ForgotPasswordResponseDto>>
        {
            private readonly UserManager<ApplicationUser> _userManager;
            private readonly Application.Services.IEmailService _emailService;
            private readonly IHttpContextAccessor _httpContextAccessor;

            public ForgotPasswordCommandHandler(
                UserManager<ApplicationUser> userManager,
                Application.Services.IEmailService emailService,
                IHttpContextAccessor httpContextAccessor)
            {
                _userManager = userManager;
                _emailService = emailService;
                _httpContextAccessor = httpContextAccessor;
            }

            public async Task<Result<ForgotPasswordResponseDto>> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
            {
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null || !user.IsActive)
                {
                    return Result<ForgotPasswordResponseDto>.Success(new ForgotPasswordResponseDto
                    {
                        Success = true,
                        Message = "If your email is registered, a reset link has been sent."
                    }, "If your email is registered, a reset link has been sent.");
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                var httpRequest = _httpContextAccessor.HttpContext?.Request;
                var baseUrl = httpRequest != null
                    ? $"{httpRequest.Scheme}://{httpRequest.Host}"
                    : "https://localhost:44397";
                var resetLink = $"{baseUrl}/reset-password?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(user.Email!)}";

                await _emailService.SendPasswordResetEmailAsync(user.Email!, resetLink);

                return Result<ForgotPasswordResponseDto>.Success(new ForgotPasswordResponseDto
                {
                    Success = true,
                    Message = "If your email is registered, a reset link has been sent.",
                    DevResetLink = resetLink,
                    DevResetToken = token
                }, "Reset link sent.");
            }
        }
    }

    public class ForgotPasswordResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? DevResetLink { get; set; }
        public string? DevResetToken { get; set; }
    }
}
