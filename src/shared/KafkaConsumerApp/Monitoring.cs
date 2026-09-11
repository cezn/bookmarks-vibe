using System.Diagnostics;

namespace KafkaConsumerApp;

internal class Monitoring
{
    internal static ActivitySource ActivitySource { get; set; } = new("KafkaConsumerApp");
}
