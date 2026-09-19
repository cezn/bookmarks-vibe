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
