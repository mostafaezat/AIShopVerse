using AIShopVerse.EndUser.Server.Controllers.Base;
using Application.DTO.AuthDtos;
using Application.Features.AuthFeatures.Commands;
using Application.Features.AuthFeatures.Queries;
using Application.Base.Wrapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AIShopVerse.EndUser.Server.Controllers.AuthControllers
{
    [EnableRateLimiting("fixed")]
    [Route("api/auth")]
    [ApiController]
    public class AuthenticationController : ApiControllerBase
    {
        [HttpPost("login")]
        public async Task<ActionResult<Result<AuthResponseDto>>> Login(LoginCommand command)
            => Single(await CommandAsync(command));

        [HttpPost("register")]
        public async Task<ActionResult<Result<AuthResponseDto>>> Register(RegisterUserCommand command)
            => Single(await CommandAsync(command));

        [HttpPost("refresh-token")]
        public async Task<ActionResult<Result<AuthResponseDto>>> RefreshToken(RefreshTokenCommand command)
            => Single(await CommandAsync(command));

        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<Result<UserDto>>> GetCurrentUser()
            => Single(await QueryAsync(new GetCurrentUserQuery()));

        [HttpPost("logout")]
        [Authorize]
        public async Task<ActionResult<Result<bool>>> Logout(LogoutCommand command)
            => Single(await CommandAsync(command));

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<ActionResult<Result<ForgotPasswordResponseDto>>> ForgotPassword(ForgotPasswordCommand command)
            => Single(await CommandAsync(command));

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<ActionResult<Result<ResetPasswordResponseDto>>> ResetPassword(ResetPasswordCommand command)
            => Single(await CommandAsync(command));

        [HttpPost("google")]
        [AllowAnonymous]
        public async Task<ActionResult<Result<AuthResponseDto>>> GoogleLogin(GoogleLoginCommand command)
            => Single(await CommandAsync(command));
    }
}
