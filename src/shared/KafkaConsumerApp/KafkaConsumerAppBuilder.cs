namespace KafkaConsumerApp;

public class KafkaConsumerAppBuilder(Dictionary<string, Dictionary<string, object>> handlers)
{
    private readonly Dictionary<string, Dictionary<string, object>> _handlers = handlers;

    public KafkaConsumerAppBuilder Handle(string topic, string type, object handler)
    {
        _handlers.TryAdd(topic, []);
        _handlers[topic][type] = handler;

        return this;
    }
}
