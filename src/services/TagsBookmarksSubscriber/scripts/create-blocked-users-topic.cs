#!/usr/local/dotnet/dotnet

#:package ConsoleAppFramework@5.7.13
#:package Confluent.Kafka@2.12.0

using Confluent.Kafka;
using Confluent.Kafka.Admin;
using ConsoleAppFramework;

await ConsoleApp.RunAsync(args, CreateBlockedUsersTopicAsync);

/// <summary>
/// Creates the blocked users topic with compact cleanup policy.
/// </summary>
/// <param name="host">-h, Kafka bootstrap host.</param>
///
/// <param name="port">-p, Kafka bootstrap port.</param>
/// <param name="topicName">-t, Topic name to create.</param>
static async Task CreateBlockedUsersTopicAsync(
    string host = "localhost",
    int port = 9092,
    string topicName = "bookmark.users.blocked"
)
{
    var adminConfig = new AdminClientConfig { BootstrapServers = $"{host}:{port}" };

    using var adminClient = new AdminClientBuilder(adminConfig).Build();

    var topic = new TopicSpecification
    {
        Name = topicName,
        NumPartitions = 1,
        ReplicationFactor = 1,
        Configs = new Dictionary<string, string> { ["cleanup.policy"] = "compact" },
    };

    try
    {
        await adminClient.CreateTopicsAsync([topic]);
        Console.WriteLine($"Topic '{topicName}' created successfully.");
    }
    catch (CreateTopicsException ex) when (ex.Results.Any(r => r.Error.Code == ErrorCode.TopicAlreadyExists))
    {
        Console.WriteLine($"Topic '{topicName}' already exists.");
    }
    catch (CreateTopicsException ex)
    {
        Console.WriteLine($"Error creating topic: {ex.Message}");
        foreach (var result in ex.Results)
            Console.WriteLine($" - {result.Topic}: {result.Error.Reason}");
    }
}
