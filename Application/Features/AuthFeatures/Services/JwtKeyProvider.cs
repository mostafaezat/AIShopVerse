using Microsoft.Extensions.Configuration;

namespace Application.Features.AuthFeatures.Services
{
    /// <summary>
    /// Single source of truth for the JWT signing key.
    /// Resolution order: <c>AISHOPVERSE_JWT_KEY</c> environment variable, then
    /// <c>Jwt:Key</c> configuration (dev-only value lives in appsettings.Development.json).
    /// Fails fast with actionable guidance when no key is configured — especially in
    /// Production, where a missing key must stop startup rather than sign tokens with
    /// an empty/default secret.
    /// </summary>
    public static class JwtKeyProvider
    {
        public const string EnvironmentVariableName = "AISHOPVERSE_JWT_KEY";

        public static string Resolve(IConfiguration configuration)
        {
            var envKey = Environment.GetEnvironmentVariable(EnvironmentVariableName);
            if (!string.IsNullOrWhiteSpace(envKey))
                return envKey;

            var configuredKey = configuration["Jwt:Key"];
            if (!string.IsNullOrWhiteSpace(configuredKey))
                return configuredKey;

            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                ?? "Production";

            if (string.Equals(environment, "Production", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "The JWT signing key is not configured in the Production environment. " +
                    $"Set the '{EnvironmentVariableName}' environment variable (never commit it) or provide 'Jwt:Key' in settings.");
            }

            throw new InvalidOperationException(
                "The JWT signing key is not configured. " +
                $"Set the '{EnvironmentVariableName}' environment variable or add 'Jwt:Key' to appsettings.Development.json.");
        }
    }
}