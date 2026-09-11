using BookmarksApi.Outbox;
using Microsoft.Extensions.Options;
using Npgsql;

namespace OutboxsApi.Outbox;

public static class OutboxServiceCollection
{
    public static IServiceCollection AddOutbox(this IServiceCollection services, IConfiguration config)
    {
        services.AddOptionsWithValidateOnStart<OutboxOptions>().Bind(config.GetSection("Outbox"));

        return services;
    }
}
