using Application.DTO.AuthDtos;
using Application.Features.AuthFeatures.Services;
using Microsoft.AspNetCore.Identity;

namespace Application.Features.AuthFeatures.Commands
{
    public class RegisterUserCommand : IRequest<Result<AuthResponseDto>>
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }

        public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result<AuthResponseDto>>
        {
            private readonly UserManager<ApplicationUser> _userManager;
            private readonly IJwtTokenService _jwtTokenService;
            private readonly IUnitOfWork _unitOfWork;

            public RegisterUserCommandHandler(
                UserManager<ApplicationUser> userManager,
                IJwtTokenService jwtTokenService,
                IUnitOfWork unitOfWork)
            {
                _userManager = userManager;
                _jwtTokenService = jwtTokenService;
                _unitOfWork = unitOfWork;
            }

            public async Task<Result<AuthResponseDto>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
            {
                var existingUser = await _userManager.FindByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    return Result<AuthResponseDto>.Falid(null, "Email already registered.");
                }

                var user = new ApplicationUser
                {
                    UserName = request.Email,
                    Email = request.Email,
                    FullName = request.FullName,
                    PhoneNumber = request.PhoneNumber,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user, request.Password);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return Result<AuthResponseDto>.Falid(null, errors);
                }

                await _userManager.AddToRoleAsync(user, "Customer");

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
                    Message = "Registration successful.",
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
                }, "Registration successful.");
            }
        }
    }
}
