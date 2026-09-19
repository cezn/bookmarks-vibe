using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace KafkaConsumerApp.Dlq;

public static class KafkaConsumerAppDlqServiceCollectionExtensions
{
    public static IHostApplicationBuilder AddKafkaConsumerDlq(
        this IHostApplicationBuilder biulder,
        IConfiguration config
    )
    {
        biulder.AddKeyedKafkaProducer<string, byte[]>(
            "dlq-producer",
            configureSettings: settings =>
            {
                settings.ConnectionString = biulder.Configuration.GetConnectionString("kafka");
            }
        );
        biulder.AddKeyedKafkaConsumer<string, byte[]>(
            "blocked-users-consumer",
            configureSettings: settings =>
            {
                settings.ConnectionString = biulder.Configuration.GetConnectionString("kafka");
                settings.Config.GroupId =
                    settings.Config.GroupId + Environment.MachineName + Guid.NewGuid().ToString("N");
                // The state topic is compacted and single-partition; we use the partition-EOF
                // event as the "fully rebuilt" signal, so it must be enabled. This runs after the
                // JSON config is bound, so it overrides any EnablePartitionEof value in config.
                // TODO: check if reliance on EOF is invalid, as there can be parallel consumers producing blocked users
                settings.Config.EnablePartitionEof = true;
            }
        );
        biulder.Services.Configure<KafkaDlqOptions>(config.GetSection("Kafka:Dlq"));
        biulder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<KafkaDlqOptions>>().Value);
        biulder.Services.AddSingleton<IBlockedUsersStore, InMemoryBlockedUsersStore>();
        biulder.Services.AddSingleton<BlockedUsersStateReady>();
        biulder.Services.AddSingleton<IDeadLetterPublisher, KafkaDeadLetterPublisher>();
        biulder.Services.AddSingleton<IKafkaDlqFacade, KafkaDlqFacade>();
        biulder.Services.AddHostedService<BlockedUsersStateBackgroundService>();

        return biulder;
    }
}
