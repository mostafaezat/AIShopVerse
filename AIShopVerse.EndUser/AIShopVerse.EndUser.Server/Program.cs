using Infrastructure;
using Application;
using Application.Features.AuthFeatures.Services;
using AIShopVerse.EndUser.Server;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Seed;
using Infrastructure.Signalr;
using Infrastructure.Services.PaymentGateway;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

try
{
    builder.Host.UseSerilog();

    builder.Services.AddControllers().AddJsonOptions(o =>
        o.JsonSerializerOptions.Converters.Add(new EnumStringOrNumberConverterFactory()));
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "AIShopVerse EndUser API", Version = "v1" });
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
        c.TagActionsBy(apiDesc =>
        {
            var controllerName = apiDesc.ActionDescriptor.RouteValues["controller"]?.ToString() ?? "Other";
            return new[] { controllerName };
        });
        c.OrderActionsBy(apiDesc => apiDesc.RelativePath);
    });

    builder.Services.AddInfrastructureDependencies(builder.Configuration);
    builder.Services.AddApplicationDependencies();
    builder.Services.AddHostedService<ExpiredPendingOrdersHostedService>();

    // Resolved at startup (fail-fast if missing) rather than at design time so
    // `dotnet ef` builds are not blocked by environment checks.
    string jwtKey = string.Empty;

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/notificationHub"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

    builder.Services.AddAuthorization();
    builder.Services.AddProblemDetails();

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAngular", policy =>
        {
            var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
            if (origins == null || origins.Length == 0)
            {
                origins = new[] { "https://localhost:4000", "http://localhost:4000" };
            }

            policy.WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });

    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.AddFixedWindowLimiter("fixed", opt =>
        {
            opt.PermitLimit = 60;
            opt.Window = TimeSpan.FromMinutes(1);
            opt.QueueLimit = 0;
        });

        options.AddFixedWindowLimiter("checkout", opt =>
        {
            opt.PermitLimit = 10;
            opt.Window = TimeSpan.FromMinutes(1);
            opt.QueueLimit = 0;
        });
    });

    var app = builder.Build();

    jwtKey = JwtKeyProvider.Resolve(app.Configuration);

    if (!app.Environment.IsDevelopment() &&
        (app.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()?.Length ?? 0) == 0)
    {
        throw new InvalidOperationException(
            "CORS is not configured for a non-development environment. " +
            "Add the exact allowed origins to 'Cors:AllowedOrigins' in your environment's settings.");
    }

    app.Use(async (context, next) =>
    {
        context.Response.Headers.Append("X-Frame-Options", "DENY");
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Append("Content-Security-Policy", "frame-ancestors 'none'");
        context.Response.Headers.Remove("Server");
        context.Response.Headers.Remove("X-Powered-By");
        await next();
    });

    app.UseExceptionHandler(appBuilder =>
    {
        appBuilder.Run(async context =>
        {
            var exception = context.Features.Get<IExceptionHandlerPathFeature>()?.Error;
            if (exception is FluentValidation.ValidationException validationException)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                context.Response.ContentType = "application/problem+json";
                var errors = validationException.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                var problem = new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Validation failed",
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1"
                };
                problem.Extensions["errors"] = errors;
                await context.Response.WriteAsJsonAsync(problem);
                return;
            }

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An error occurred while processing your request.",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1"
            });
        });
    });

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseCors("AllowAngular");
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapHub<NotificationHub>("/notificationHub");
    app.MapControllers();

    if (app.Environment.IsDevelopment())
    {
        // Proxy only non-API requests to the Angular dev server so the backend
        // API stays directly reachable (e.g. for testing) without `ng serve`.
        app.MapWhen(ctx => !ctx.Request.Path.StartsWithSegments("/api"), spaBranch =>
        {
            spaBranch.UseSpa(spa =>
            {
                spa.Options.SourcePath = "../AIShopVerse.EndUser.client";
                spa.UseProxyToSpaDevelopmentServer("https://localhost:4000");
            });
        });
    }
    else
    {
        // Production: the SPA is published into wwwroot by the PublishRunWebpack
        // target in this project's csproj; fall back to index.html so client-side
        // routes resolve after refresh.
        app.MapFallbackToFile("index.html");
    }

    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate();
        await SeedData.InitializeAsync(services);
        await CatalogSeed.InitializeAsync(context);
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
