using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Services.FileStorage;
using Infrastructure.Services.PaymentGateway;
using Infrastructure.Signalr;
using Infrastructure.Services.Realtime;
using Infrastructure.Common;
using Infrastructure.Services;

namespace Infrastructure
{
    public static class InfrastructureDependencyInjection
    {
        public static IServiceCollection AddInfrastructureDependencies(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"),
                    builder => builder.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

            services.AddIdentity<ApplicationUser, ApplicationRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            services.Configure<IdentityOptions>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;
                options.User.RequireUniqueEmail = true;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            });

            services.AddTransient<IUnitOfWork, UnitOfWork>();
            services.AddTransient(typeof(IRepositoryBase<>), typeof(RepositoryBase<>));

            services.Configure<FileStorageSettings>(configuration.GetSection("FileStorage"));
            services.AddScoped<IFileRepository, LocalFileRepository>();

            services.Configure<StripeOptions>(configuration.GetSection("Stripe"));
            services.AddScoped<IStripePaymentService, StripePaymentService>();

            services.Configure<InventoryOptions>(configuration.GetSection("Inventory"));
            services.Configure<PaymentOptions>(configuration.GetSection("Payments"));
            services.AddScoped<IExpiredOrderProcessor, ExpiredOrderProcessor>();

            var relayOptions = configuration.GetSection("Redis").Get<RedisRelayOptions>() ?? new RedisRelayOptions();
            relayOptions.HostName = string.IsNullOrWhiteSpace(relayOptions.HostName) ? "host" : relayOptions.HostName;
            services.AddSingleton(relayOptions);

            var signalR = services.AddSignalR();
            if (relayOptions.Enabled)
            {
                signalR.AddStackExchangeRedis(relayOptions.Configuration!);
            }

            services.AddSingleton<Microsoft.AspNetCore.SignalR.IUserIdProvider, CustomUserIdProvider>();
            services.AddSingleton<IConnectedUserTracker, ConnectedUserTracker>();
            services.AddScoped<IRealtimeNotifier, SignalRRealtimeNotifier>();
            services.AddHostedService<RealtimeRelayHostedService>();

            return services;
        }
    }
}
