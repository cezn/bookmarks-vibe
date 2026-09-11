namespace KafkaConsumerApp;

[Serializable]
internal class KafkaMessageDispatchException : Exception
{
    public KafkaMessageDispatchException() { }

    public KafkaMessageDispatchException(string? message)
        : base(message) { }

    public KafkaMessageDispatchException(string? message, Exception? innerException)
        : base(message, innerException) { }

    public string? Topic { get; init; }
    public string? Type { get; init; }
}
