using Application.Base.Wrapper;
using Application.DTO.AuthDtos;
using Application.Features.AuthFeatures.Commands;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Application.Tests.Auth
{
    public class RefreshTokenFlowTests
    {
        private const string Email = "test@example.com";
        private const string Password = "Str0ng!Passw0rd";

        [Fact]
        public async Task Login_WithValidCredentials_ReturnsTokens()
        {
            using var db = new TestDb();
            await db.CreateUserAsync(Email, Password);

            var result = await db.LoginAsync(Email, Password);

            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.False(string.IsNullOrWhiteSpace(result.Data.Token));
            Assert.False(string.IsNullOrWhiteSpace(result.Data.RefreshToken));
        }

        [Fact]
        public async Task Login_WithInvalidPassword_Fails()
        {
            using var db = new TestDb();
            await db.CreateUserAsync(Email, Password);

            var result = await db.LoginAsync(Email, "Wrong!Passw0rd");

            Assert.False(result.IsSuccess);
            Assert.Null(result.Data);
        }

        [Fact]
        public async Task Refresh_RotatesToken_AndRevokesOldOne()
        {
            using var db = new TestDb();
            await db.CreateUserAsync(Email, Password);

            var login = await db.LoginAsync(Email, Password);
            var firstRefresh = login.Data!.RefreshToken!;
            var firstAccess = login.Data.Token!;

            var refresh = await db.Mediator.Send(new RefreshTokenCommand
            {
                Token = firstAccess,
                RefreshToken = firstRefresh
            });

            Assert.True(refresh.IsSuccess);
            Assert.NotNull(refresh.Data);
            Assert.NotEqual(firstRefresh, refresh.Data.RefreshToken);

            var storedOld = await db.DbContext.RefreshTokens.FirstAsync(rt => rt.Token == firstRefresh);
            Assert.NotNull(storedOld.Revoked);
            Assert.Equal(refresh.Data.RefreshToken, storedOld.ReplacedByToken);
        }

        [Fact]
        public async Task Refresh_WithReusedToken_FailsAndRevokesFamily()
        {
            using var db = new TestDb();
            await db.CreateUserAsync(Email, Password);

            var login = await db.LoginAsync(Email, Password);
            var firstRefresh = login.Data!.RefreshToken!;
            var firstAccess = login.Data.Token!;

            var firstRefreshResult = await db.Mediator.Send(new RefreshTokenCommand
            {
                Token = firstAccess,
                RefreshToken = firstRefresh
            });
            Assert.True(firstRefreshResult.IsSuccess);

            var reuse = await db.Mediator.Send(new RefreshTokenCommand
            {
                Token = firstAccess,
                RefreshToken = firstRefresh
            });

            Assert.False(reuse.IsSuccess);
            Assert.Null(reuse.Data);

            var user = await db.UserManager.FindByEmailAsync(Email);
            var userTokens = await db.DbContext.RefreshTokens
                .Where(rt => rt.UserId == user!.Id)
                .ToListAsync();
            Assert.NotEmpty(userTokens);
            Assert.All(userTokens, rt => Assert.NotNull(rt.Revoked));
        }

        [Fact]
        public async Task Refresh_WithUnknownRefreshToken_Fails()
        {
            using var db = new TestDb();
            await db.CreateUserAsync(Email, Password);

            var login = await db.LoginAsync(Email, Password);

            var result = await db.Mediator.Send(new RefreshTokenCommand
            {
                Token = login.Data!.Token!,
                RefreshToken = "totally-unknown-token"
            });

            Assert.False(result.IsSuccess);
            Assert.Null(result.Data);
        }

        [Fact]
        public async Task Refresh_WithExpiredRefreshToken_Fails()
        {
            using var db = new TestDb();
            await db.CreateUserAsync(Email, Password);

            var login = await db.LoginAsync(Email, Password);
            var refreshToken = login.Data!.RefreshToken!;

            var stored = await db.DbContext.RefreshTokens.FirstAsync(rt => rt.Token == refreshToken);
            stored.Expires = DateTime.UtcNow.AddMinutes(-1);
            await db.DbContext.SaveChangesAsync();

            var result = await db.Mediator.Send(new RefreshTokenCommand
            {
                Token = login.Data.Token!,
                RefreshToken = refreshToken
            });

            Assert.False(result.IsSuccess);
            Assert.Null(result.Data);
        }
    }
}