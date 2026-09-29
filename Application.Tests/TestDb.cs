using Application.Base.Wrapper;
using Application.DTO.AuthDtos;
using Application.Features.AuthFeatures.Commands;
using Domain.Entities.Identity;
using Infrastructure;
using Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Tests
{
    /// <summary>
    /// Creates an isolated SQL Server database (per instance unless a connection
    /// string is supplied) with the same DI wiring used by the hosts. Database
    /// name honors an optional AISHOPVERSE_TEST_CONNECTION base connection string.
    /// </summary>
    public sealed class TestDb : IDisposable
    {
        private readonly TestCurrentUser _currentUser;

        public ServiceProvider Services { get; }
        public ApplicationDbContext DbContext { get; }
        public ISender Mediator { get; }
        public UserManager<ApplicationUser> UserManager { get; }
        public IConfiguration Configuration { get; }

        public TestQueryCounter QueryCounter { get; } = new();

        public TestDb(string? connectionString = null)
        {
            var baseConnection = Environment.GetEnvironmentVariable("AISHOPVERSE_TEST_CONNECTION")
                ?? "Server=.;Trusted_Connection=True;TrustServerCertificate=True;";
            var connectionStringToUse = string.IsNullOrWhiteSpace(connectionString)
                ? new SqlConnectionStringBuilder(baseConnection)
                {
                    InitialCatalog = "AIShopVerseTests_" + Guid.NewGuid().ToString("N")
                }.ConnectionString
                : connectionString;

            var configValues = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionStringToUse,
                ["Jwt:Key"] = "test-signing-key-0123456789abcdef0123456789abcdef",
                ["Jwt:Issuer"] = "AIShopVerseTests",
                ["Jwt:Audience"] = "AIShopVerseTests",
                ["Jwt:ExpirationInMinutes"] = "5"
            };
            Configuration = new TestConfiguration(configValues);

            _currentUser = new TestCurrentUser();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(Configuration);
            services.AddLogging();
            services.AddSingleton<IInterceptor>(QueryCounter);
            services.AddInfrastructureDependencies(Configuration);
            services.AddApplicationDependencies();
            services.AddSingleton<ICurrentUserService>(_currentUser);
            Services = services.BuildServiceProvider();

            DbContext = Services.GetRequiredService<ApplicationDbContext>();
            DbContext.Database.EnsureCreated();

            Mediator = Services.GetRequiredService<ISender>();
            UserManager = Services.GetRequiredService<UserManager<ApplicationUser>>();
        }

        public string ConnectionString => DbContext.Database.GetDbConnection().ConnectionString;

        public void SetCurrentUser(string? userId) => _currentUser.UserId = userId;

        private sealed class TestCurrentUser : ICurrentUserService
        {
            public string? UserId { get; set; }
            public string? UserName => "Test User";
            public bool IsAuthenticated => true;
        }

        public async Task<ApplicationUser> CreateUserAsync(
            string email = "test@example.com",
            string password = "Str0ng!Passw0rd")
        {
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = "Test User",
                PhoneNumber = "01000000001",
                IsActive = true
            };
            var result = await UserManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"User creation failed: {string.Join("; ", result.Errors.Select(e => e.Description))}");
            }
            return user;
        }

        public Task<Result<AuthResponseDto>> LoginAsync(string email, string password)
            => Mediator.Send(new LoginCommand { Email = email, Password = password });

        public void Dispose()
        {
            try
            {
                DbContext.Database.EnsureDeleted();
            }
            catch
            {
                // Database might already be gone; the test server is shared.
            }
            DbContext.Dispose();
            Services.Dispose();
        }
    }
}