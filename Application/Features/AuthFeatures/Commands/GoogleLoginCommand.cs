using Application.DTO.AuthDtos;
using Application.Features.AuthFeatures.Services;
using Microsoft.AspNetCore.Identity;

namespace Application.Features.AuthFeatures.Commands
{
    public class GoogleLoginCommand : IRequest<Result<AuthResponseDto>>
    {
        public string IdToken { get; set; } = string.Empty;

        public class GoogleLoginCommandHandler : IRequestHandler<GoogleLoginCommand, Result<AuthResponseDto>>
        {
            private readonly UserManager<ApplicationUser> _userManager;
            private readonly IJwtTokenService _jwtTokenService;
            private readonly IUnitOfWork _unitOfWork;

            public GoogleLoginCommandHandler(
                UserManager<ApplicationUser> userManager,
                IJwtTokenService jwtTokenService,
                IUnitOfWork unitOfWork)
            {
                _userManager = userManager;
                _jwtTokenService = jwtTokenService;
                _unitOfWork = unitOfWork;
            }

            public async Task<Result<AuthResponseDto>> Handle(GoogleLoginCommand request, CancellationToken cancellationToken)
            {
                var payload = await VerifyGoogleTokenAsync(request.IdToken);
                if (payload == null)
                    return Result<AuthResponseDto>.Falid(null, "Invalid Google token.");

                var email = payload.Email;
                var fullName = payload.Name ?? payload.GivenName ?? email;
                var picture = payload.Picture;

                if (string.IsNullOrEmpty(email))
                    return Result<AuthResponseDto>.Falid(null, "Email not provided by Google.");

                var user = await _userManager.FindByEmailAsync(email);

                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        FullName = fullName,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    var createResult = await _userManager.CreateAsync(user);
                    if (!createResult.Succeeded)
                    {
                        var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                        return Result<AuthResponseDto>.Falid(null, errors);
                    }

                    await _userManager.AddToRoleAsync(user, "Customer");
                }
                else if (!user.IsActive)
                {
                    return Result<AuthResponseDto>.Falid(null, "Account is disabled.");
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

            private static async Task<GooglePayload?> VerifyGoogleTokenAsync(string idToken)
            {
                try
                {
                    var settings = new Google.Apis.Auth.GoogleJsonWebSignature.ValidationSettings
                    {
                        Audience = new[] { Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID") ?? "" }
                    };
                    var payload = await Google.Apis.Auth.GoogleJsonWebSignature.ValidateAsync(idToken, settings);
                    return new GooglePayload
                    {
                        Email = payload.Email,
                        Name = payload.Name,
                        GivenName = payload.GivenName,
                        Picture = payload.Picture
                    };
                }
                catch
                {
                    return null;
                }
            }
        }
    }

    internal class GooglePayload
    {
        public string? Email { get; set; }
        public string? Name { get; set; }
        public string? GivenName { get; set; }
        public string? Picture { get; set; }
    }
}
