#!/usr/local/dotnet/dotnet

#:package ConsoleAppFramework@5.7.13
#:package Confluent.Kafka@2.12.0

using Confluent.Kafka;
using Confluent.Kafka.Admin;
using ConsoleAppFramework;

await ConsoleApp.RunAsync(args, CreateDlqTopicsAsync);

/// <summary>
/// Creates the DLQ and blocked users topics, then validates their creation.
/// </summary>
/// <param name="host">-h, Kafka bootstrap host.</param>
/// <param name="port">-p, Kafka bootstrap port.</param>@
static async Task CreateDlqTopicsAsync(string host = "localhost", int port = 9092)
{
    var bootstrapServers = $"{host}:{port}";
    var topics = new[]
    {
        new TopicSpecification
        {
            Name = "outbox.event.Bookmark.dlq",
            NumPartitions = 1,
            ReplicationFactor = 1,
            Configs = new Dictionary<string, string> { ["cleanup.policy"] = "delete", ["retention.ms"] = "604800000" },
        },
        new TopicSpecification
        {
            Name = "bookmark.users.blocked",
            NumPartitions = 1,
            ReplicationFactor = 1,
            Configs = new Dictionary<string, string> { ["cleanup.policy"] = "compact" },
        },
    };

    var topicNames = topics.Select(t => t.Name).ToArray();

    var adminConfig = new AdminClientConfig { BootstrapServers = bootstrapServers };
    using var adminClient = new AdminClientBuilder(adminConfig).Build();

    try
    {
        await adminClient.CreateTopicsAsync(topics);
        Console.WriteLine($"Created topics on {bootstrapServers}:");
        foreach (var topic in topicNames)
            Console.WriteLine($" - {topic}");
    }
    catch (CreateTopicsException ex)
    {
        var hasNonExistsErrors = ex.Results.Any(r => r.Error.Code != ErrorCode.TopicAlreadyExists);
        if (hasNonExistsErrors)
        {
            Console.WriteLine($"Error creating topics on {bootstrapServers}: {ex.Message}");
            foreach (var result in ex.Results)
                Console.WriteLine($" - {result.Topic}: {result.Error.Reason}");
            return;
        }

        Console.WriteLine($"All required topics already exist on {bootstrapServers}.");
    }

    var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(5));
    var missingTopics = topicNames
        .Where(name => metadata.Topics.All(t => !string.Equals(t.Topic, name, StringComparison.Ordinal)))
        .ToArray();

    if (missingTopics.Length == 0)
    {
        Console.WriteLine("Topic validation succeeded:");
        foreach (var topic in topicNames)
            Console.WriteLine($" - {topic}");
    }
    else
    {
        Console.WriteLine("Topic validation failed. Missing topics:");
        foreach (var missing in missingTopics)
            Console.WriteLine($" - {missing}");
    }
}
