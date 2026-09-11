using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

namespace ServiceDefaults;

public static class OpenApiExtensions
{
    public static IServiceCollection AddCustomOpenApi(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<OpenApiOptions>? configureOptions = null
    )
    {
        services.AddOpenApi(x =>
        {
            x.AddDocumentTransformer<JwtDocumentOpenApiTransformer>();
            configureOptions?.Invoke(x);
        });

        services.AddCors(options =>
        {
            options.AddPolicy(
                "AllowSwagger",
                policy =>
                    policy
                        .WithOrigins(configuration.GetValue("SwaggerUI:URL", "http://localhost:9999"))
                        .AllowAnyMethod()
                        .AllowAnyHeader()
            );
            options.DefaultPolicyName = "AllowSwagger";
        });

        return services;
    }
}

public class JwtDocumentOpenApiTransformer(IHostEnvironment env, IConfiguration config) : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken
    )
    {
        document.Components ??= new OpenApiComponents();

        document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
        {
            ["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                In = ParameterLocation.Header,
                BearerFormat = "Json Web Token",
                Description = env.IsDevelopment()
                    ? $"""
                        Bearer JWT authentication.
                        A development token: **{GenerateDevJwt(config.GetSection("Jwt")) ?? "not-found"}**"
                        """
                    : "Bearer JWT authentication.",
            },
        };

        foreach (var operation in document.Paths.Values.SelectMany(path => path.Operations!))
        {
            operation.Value.Security ??= [];
            operation.Value.Security.Add(
                new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference("Bearer", document)] = [] }
            );
        }

        return Task.CompletedTask;
    }

    static string? GenerateDevJwt(IConfigurationSection jwtSection)
    {
        var key = jwtSection.GetValue<string>("Key");
        var issuer = jwtSection.GetValue<string>("Issuer");
        var audience = jwtSection.GetValue<string>("Audience");

        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
            return null;

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256
        );

        var handler = new JsonWebTokenHandler();
        var subject = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "570caa91-8a71-467c-9ff7-1e104d84b2f1")],
            "dev-jwt"
        );

        return handler.CreateToken(
            new SecurityTokenDescriptor
            {
                Issuer = issuer,
                Audience = audience,
                Expires = DateTime.UtcNow.AddHours(8),
                Subject = subject,
                SigningCredentials = credentials,
            }
        );
    }
}
