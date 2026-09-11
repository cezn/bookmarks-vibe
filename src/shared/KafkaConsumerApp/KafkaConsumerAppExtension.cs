using Confluent.Kafka;
using KafkaConsumerApp.Dlq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KafkaConsumerApp;

public static class KafkaConsumerAppExtension
{
    public static KafkaConsumerAppBuilder AddKafkaConsumerApp(
        this IHostApplicationBuilder builder,
        IConfiguration config
    )
    {
        Dictionary<string, Dictionary<string, object>> handlers = [];
        builder.AddKafkaConsumer<string, byte[]>(
            "kafka",
            configureBuilder: (services, consumerBuilder) =>
            {
                var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Kafka.Consumer");

                consumerBuilder
                    // .SetLogHandler(
                    //     (c, msg) =>
                    //     {
                    //         var level = MapLogLevel(msg.Level);
                    //         if (!logger.IsEnabled(level))
                    //             return;

                    //         logger.Log(
                    //             logLevel: level,
                    //             eventId: default,
                    //             message: "[{Facility}] {Message}",
                    //             args: [msg.Facility, msg.Message]
                    //         );

                    //         static LogLevel MapLogLevel(SyslogLevel level) =>
                    //             level switch
                    //             {
                    //                 SyslogLevel.Emergency => LogLevel.Critical,
                    //                 SyslogLevel.Alert => LogLevel.Critical,
                    //                 SyslogLevel.Critical => LogLevel.Critical,
                    //                 SyslogLevel.Error => LogLevel.Error,
                    //                 SyslogLevel.Warning => LogLevel.Warning,
                    //                 SyslogLevel.Notice => LogLevel.Information,
                    //                 SyslogLevel.Info => LogLevel.Information,
                    //                 SyslogLevel.Debug => LogLevel.Debug,
                    //                 _ => LogLevel.Information,
                    //             };
                    //     }
                    // )
                    .SetPartitionsAssignedHandler(
                        (_, partitions) =>
                            logger.LogInformation("Partitions assigned: [{Partitions}]", string.Join(", ", partitions))
                    )
                    .SetPartitionsRevokedHandler(
                        (c, partitions) =>
                        {
                            // Rebalance started - consumer lost partitions after '.Cnsume()' call.
                            // Commit offsets before allowing consumer to proceed with rebalance.
                            logger.LogInformation("Partitions revoked: [{Partitions}]", string.Join(", ", partitions));
                            c.Commit();
                        }
                    )
                    .SetPartitionsLostHandler(
                        (_, partitions) =>
                            logger.LogInformation("Partitions lost: [{Partitions}]", string.Join(", ", partitions))
                    )
                    .Build();
            }
        );
        builder.AddKafkaConsumerDlq(config);
        builder.Services.AddKeyedSingleton("KafkaConsumerAppHandlers", handlers);
        builder.Services.AddSingleton<KafkaMessageDispatcher>();
        builder.Services.AddSingleton<KafkaMessageWorkerLoop>();
        builder.Services.AddHostedService<KafkaBackgroundService>();
        builder
            .Services.AddOpenTelemetry()
            .WithTracing(x => x.AddSource(Monitoring.ActivitySource.Name))
            .WithMetrics(x => x.AddMeter(Monitoring.ActivitySource.Name));

        return new(handlers);
    }
}
