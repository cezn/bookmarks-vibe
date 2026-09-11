using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace SummarizeApi.Setup;

public static class AuthExtensions
{
    public static IServiceCollection AddCustomAuth(this IServiceCollection services, IConfiguration jwtConfig)
    {
        var authBuilder = services.AddAuthentication();

        // Add JWT Bearer authentication for user-facing endpoints
        SetupJwtAuthentication(jwtConfig, authBuilder);

        services.AddAuthorization(x =>
        {
            x.AddPolicy(
                "RequireJWT",
                b => b.RequireAuthenticatedUser().AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
            );
            x.DefaultPolicy = x.GetPolicy("RequireJWT")!;
            x.FallbackPolicy = x.DefaultPolicy;
        });

        return services;
    }

    private static void SetupJwtAuthentication(IConfiguration jwtConfig, AuthenticationBuilder authBuilder)
    {
        authBuilder.AddJwtBearer(x =>
        {
            var jwtAudience =
                jwtConfig["Audience"] ?? throw new InvalidOperationException("Audience configuration is required.");
            var jwtIssuer =
                jwtConfig["Issuer"] ?? throw new InvalidOperationException("Issuer configuration is required.");
            var jwtKey = jwtConfig["Key"] ?? throw new InvalidOperationException("Key configuration is required.");

            x.Audience = jwtAudience;
            x.RequireHttpsMetadata = false;
            x.TokenValidationParameters = new()
            {
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,
                ValidateAudience = true,
                ValidAudience = jwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            };
        });
    }
}
