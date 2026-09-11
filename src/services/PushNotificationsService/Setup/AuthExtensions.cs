using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace PushNotificationsService.Setup;

public static class AuthExtensions
{
    public static IServiceCollection AddCustomAuth(this IServiceCollection services, IConfiguration jwtConfig)
    {
        services
            .AddAuthentication()
            .AddJwtBearer(x =>
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

        services.AddAuthorization(x => x.FallbackPolicy = x.DefaultPolicy);

        return services;
    }
}
