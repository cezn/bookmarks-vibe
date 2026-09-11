#!/usr/local/dotnet/dotnet

#:package ConsoleAppFramework@5.7.13
#:package Confluent.Kafka@2.12.0

using Confluent.Kafka;
using Confluent.Kafka.Admin;
using ConsoleAppFramework;

await ConsoleApp.RunAsync(args, DeleteKafkaTopicAsync);

/// <summary>
/// Deletes the requested topic using the configured admin client.
/// </summary>
/// <param name="host">-h, Kafka bootstrap host.</param>
/// <param name="port">-p, Kafka bootstrap port.</param>
/// <param name="topicName">-t, Topic name to remove.</param>
static async Task DeleteKafkaTopicAsync(
    string host = "localhost",
    int port = 9092,
    string topicName = "outbox.event.Bookmark"
)
{
    var adminConfig = new AdminClientConfig { BootstrapServers = $"{host}:{port}" };

    using var adminClient = new AdminClientBuilder(adminConfig).Build();

    try
    {
        await adminClient.DeleteTopicsAsync([topicName]);
        Console.WriteLine($"Topic '{topicName}' deleted successfully.");
    }
    catch (DeleteTopicsException ex)
    {
        Console.WriteLine($"Error deleting topic: {ex.Message}");
    }
}
