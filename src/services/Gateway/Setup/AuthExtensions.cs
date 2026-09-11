using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.StackExchangeRedis;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Gateway.Setup;

public static class AuthExtensions
{
    public static IServiceCollection AddCustomAuth(this IServiceCollection services, string? redisConnectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(redisConnectionString);
        services.AddDataProtection().SetApplicationName("bookmarks-auth");
        services.ConfigureDataProtectionRedis();
        services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = "Identity.Application";
                options.DefaultChallengeScheme = "Identity.Application";
            })
            .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("BasicAuthentication", null)
            .AddCookie(
                "Identity.Application",
                options =>
                {
                    options.LoginPath = "/auth/account/login";
                    options.Cookie.Name = "bookmarks-auth";
                }
            );
        services.AddAuthorization(options => options.FallbackPolicy = options.DefaultPolicy);

        return services;
    }

    public static IServiceCollection ConfigureDataProtectionRedis(this IServiceCollection services)
    {
        services.AddSingleton<IConfigureOptions<KeyManagementOptions>>(sp =>
        {
            var redis = sp.GetRequiredService<IConnectionMultiplexer>();
            return new ConfigureOptions<KeyManagementOptions>(opt =>
            {
                opt.XmlRepository = new RedisXmlRepository(() => redis.GetDatabase(), "DataProtection-Keys");
            });
        });

        return services;
    }
}
