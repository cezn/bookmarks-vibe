using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.StackExchangeRedis;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Auth.Setup;

public static class DataProtectionExtensions
{
    public static IServiceCollection AddAndConfigureDataProtection(
        this IServiceCollection services,
        string? redisConnectionString
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(redisConnectionString);
        services.AddDataProtection().SetApplicationName("bookmarks-auth");
        services.ConfigureDataProtectionRedis();
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
