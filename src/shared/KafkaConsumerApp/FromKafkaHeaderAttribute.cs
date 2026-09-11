namespace KafkaConsumerApp;

[AttributeUsage(AttributeTargets.Parameter)]
public class FromKafkaHeaderAttribute(string? headerName = null) : Attribute
{
    public string? HeaderName { get; } = headerName;
}
