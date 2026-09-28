using Application.DTO.AuthDtos;
using Application.Features.AuthFeatures.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Application.Features.AuthFeatures.Commands
{
    public class RefreshTokenCommand : IRequest<Result<AuthResponseDto>>
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;

        public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResponseDto>>
        {
            private readonly UserManager<ApplicationUser> _userManager;
            private readonly IJwtTokenService _jwtTokenService;
            private readonly IConfiguration _configuration;
            private readonly IUnitOfWork _unitOfWork;

            public RefreshTokenCommandHandler(
                UserManager<ApplicationUser> userManager,
                IJwtTokenService jwtTokenService,
                IConfiguration configuration,
                IUnitOfWork unitOfWork)
            {
                _userManager = userManager;
                _jwtTokenService = jwtTokenService;
                _configuration = configuration;
                _unitOfWork = unitOfWork;
            }

            public async Task<Result<AuthResponseDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
            {
                var principal = GetPrincipalFromExpiredToken(request.Token);
                if (principal == null)
                {
                    return Result<AuthResponseDto>.Falid(null, "Invalid token.");
                }

                var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Result<AuthResponseDto>.Falid(null, "Invalid token.");
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Result<AuthResponseDto>.Falid(null, "User not found.");
                }

                var storedRefreshToken = await _unitOfWork.Repository<Domain.Entities.Identity.RefreshToken>()
                    .FindByCondition(rt => rt.Token == request.RefreshToken && rt.UserId == userId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (storedRefreshToken == null)
                {
                    return Result<AuthResponseDto>.Falid(null, "Invalid refresh token.");
                }

                if (storedRefreshToken.Revoked != null)
                {
                    // Token reuse detected: this refresh token was already rotated or revoked.
                    // Revoke the user's remaining active refresh tokens so a stolen token
                    // cannot keep the session alive after rotation.
                    var activeTokens = await _unitOfWork.Repository<Domain.Entities.Identity.RefreshToken>()
                        .FindByCondition(rt => rt.UserId == userId && rt.Revoked == null)
                        .ToListAsync(cancellationToken);

                    foreach (var token in activeTokens)
                    {
                        token.Revoked = DateTime.UtcNow;
                        _unitOfWork.Repository<Domain.Entities.Identity.RefreshToken>().Update(token);
                    }

                    await _unitOfWork.CompleteAsync(cancellationToken);
                    return Result<AuthResponseDto>.Falid(null, "Invalid refresh token.");
                }

                if (storedRefreshToken.Expires <= DateTime.UtcNow)
                {
                    return Result<AuthResponseDto>.Falid(null, "Invalid refresh token.");
                }

                var newToken = await _jwtTokenService.GenerateJwtTokenAsync(user);
                var newRefreshToken = _jwtTokenService.GenerateRefreshToken();

                storedRefreshToken.Revoked = DateTime.UtcNow;
                storedRefreshToken.ReplacedByToken = newRefreshToken;
                _unitOfWork.Repository<Domain.Entities.Identity.RefreshToken>().Update(storedRefreshToken);

                var newRefreshTokenEntity = new Domain.Entities.Identity.RefreshToken
                {
                    Token = newRefreshToken,
                    Expires = DateTime.UtcNow.AddDays(7),
                    Created = DateTime.UtcNow,
                    UserId = user.Id
                };

                _unitOfWork.Repository<Domain.Entities.Identity.RefreshToken>().Create(newRefreshTokenEntity);
                await _unitOfWork.CompleteAsync(cancellationToken);

                var roles = await _userManager.GetRolesAsync(user);

                return Result<AuthResponseDto>.Success(new AuthResponseDto
                {
                    Success = true,
                    Message = "Token refreshed.",
                    Token = newToken,
                    RefreshToken = newRefreshToken,
                    ExpiresAt = _jwtTokenService.GetTokenExpiration(),
                    User = new UserDto
                    {
                        Id = user.Id,
                        FullName = user.FullName,
                        Email = user.Email,
                        PhoneNumber = user.PhoneNumber,
                        Roles = roles.ToList()
                    }
                });
            }

            private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
            {
                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKeyProvider.Resolve(_configuration))),
                    ValidateLifetime = false,
                    ValidIssuer = _configuration["Jwt:Issuer"],
                    ValidAudience = _configuration["Jwt:Audience"]
                };

                var tokenHandler = new JwtSecurityTokenHandler();
                try
                {
                    var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
                    if (securityToken is not JwtSecurityToken jwtToken ||
                        !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return null;
                    }
                    return principal;
                }
                catch
                {
                    return null;
                }
            }
        }
    }
}
