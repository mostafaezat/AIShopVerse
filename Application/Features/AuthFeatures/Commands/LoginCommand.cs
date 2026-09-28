using Application.DTO.AuthDtos;
using Application.Features.AuthFeatures.Services;
using Microsoft.AspNetCore.Identity;

namespace Application.Features.AuthFeatures.Commands
{
    public class LoginCommand : IRequest<Result<AuthResponseDto>>
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResponseDto>>
        {
            private readonly UserManager<ApplicationUser> _userManager;
            private readonly SignInManager<ApplicationUser> _signInManager;
            private readonly IJwtTokenService _jwtTokenService;
            private readonly IUnitOfWork _unitOfWork;

            public LoginCommandHandler(
                UserManager<ApplicationUser> userManager,
                SignInManager<ApplicationUser> signInManager,
                IJwtTokenService jwtTokenService,
                IUnitOfWork unitOfWork)
            {
                _userManager = userManager;
                _signInManager = signInManager;
                _jwtTokenService = jwtTokenService;
                _unitOfWork = unitOfWork;
            }

            public async Task<Result<AuthResponseDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
            {
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null || !user.IsActive)
                {
                    return Result<AuthResponseDto>.Falid(null, "Invalid email or password.");
                }

                var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, true);
                if (!result.Succeeded)
                {
                    return Result<AuthResponseDto>.Falid(null, "Invalid email or password.");
                }

                var token = await _jwtTokenService.GenerateJwtTokenAsync(user);
                var refreshToken = _jwtTokenService.GenerateRefreshToken();

                var refreshTokenEntity = new Domain.Entities.Identity.RefreshToken
                {
                    Token = refreshToken,
                    Expires = DateTime.UtcNow.AddDays(7),
                    Created = DateTime.UtcNow,
                    UserId = user.Id
                };

                _unitOfWork.Repository<Domain.Entities.Identity.RefreshToken>().Create(refreshTokenEntity);
                await _unitOfWork.CompleteAsync(cancellationToken);

                var roles = await _userManager.GetRolesAsync(user);

                return Result<AuthResponseDto>.Success(new AuthResponseDto
                {
                    Success = true,
                    Message = "Login successful.",
                    Token = token,
                    RefreshToken = refreshToken,
                    ExpiresAt = _jwtTokenService.GetTokenExpiration(),
                    User = new UserDto
                    {
                        Id = user.Id,
                        FullName = user.FullName,
                        Email = user.Email,
                        PhoneNumber = user.PhoneNumber,
                        Roles = roles.ToList()
                    }
                }, "Login successful.");
            }
        }
    }
}
