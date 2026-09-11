using System.Reflection;
using Confluent.Kafka;
using Confluent.SchemaRegistry.Serdes;
using Microsoft.Extensions.DependencyInjection;

namespace KafkaConsumerApp;

public class KafkaMessageDispatcher(IServiceScopeFactory serviceScopeFactory)
{
    public async Task DispatchMessage(
        string topic,
        string type,
        Message<string, byte[]> message,
        Dictionary<string, Dictionary<string, object>> handlers
    )
    {
        var delegateInstance = ResolveHandler(topic, type, handlers);
        var invokeMethod = delegateInstance.Method;
        var parameters = invokeMethod.GetParameters();
        if (parameters.Length == 0)
            throw new KafkaMessageDispatchException("Handler must have at least one parameter");

        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var deserializedMessage = await DeserializeMessageAsync(scope.ServiceProvider, parameters[0], message, type);
        var handlerArgs = ResolveHandlerArguments(
            scope.ServiceProvider,
            parameters,
            deserializedMessage,
            message,
            topic,
            type
        );

        var handlerTarget =
            delegateInstance.Target ?? throw new KafkaMessageDispatchException("Handler delegate has no target");
        var result = invokeMethod.Invoke(handlerTarget, handlerArgs);
        if (result is Task task)
            await task;
    }

    private static Delegate ResolveHandler(
        string topic,
        string type,
        Dictionary<string, Dictionary<string, object>> handlers
    )
    {
        if (!handlers.TryGetValue(topic, out var typeHandler))
            throw new KafkaMessageDispatchException("Topic not found") { Topic = topic };
        if (!typeHandler.TryGetValue(type, out var handler))
            throw new KafkaMessageDispatchException("Type not found") { Topic = topic, Type = type };

        return (Delegate)handler;
    }

    private static async Task<object> DeserializeMessageAsync(
        IServiceProvider serviceProvider,
        ParameterInfo messageParameter,
        Message<string, byte[]> message,
        string type
    )
    {
        var deserializerType = typeof(ProtobufDeserializer<>).MakeGenericType(messageParameter.ParameterType);
        var deserializer = serviceProvider.GetRequiredService(deserializerType);

        var deserializeMethod = deserializerType.GetMethod(
            "DeserializeAsync",
            BindingFlags.Public | BindingFlags.Instance
        );

        if (deserializeMethod is null)
            throw new KafkaMessageDispatchException("Unable to find DeserializeAsync method on ProtobufDeserializer");

        return await (dynamic)
            deserializeMethod.Invoke(
                deserializer,
                [
                    new ReadOnlyMemory<byte>(message.Value),
                    false,
                    new SerializationContext(MessageComponentType.Value, type.Replace('_', '-')),
                ]
            )!;
    }

    private static object[] ResolveHandlerArguments(
        IServiceProvider serviceProvider,
        ParameterInfo[] parameters,
        object deserializedMessage,
        Message<string, byte[]> message,
        string topic,
        string type
    )
    {
        var handlerArgs = new object[parameters.Length];
        handlerArgs[0] = deserializedMessage;

        for (int i = 1; i < parameters.Length; i++)
            handlerArgs[i] = ResolveParameterValue(serviceProvider, parameters[i], message, topic, type);

        return handlerArgs;
    }

    private static object ResolveParameterValue(
        IServiceProvider serviceProvider,
        ParameterInfo parameter,
        Message<string, byte[]> message,
        string topic,
        string type
    )
    {
        var kafkaHeaderAttr = parameter.GetCustomAttribute<FromKafkaHeaderAttribute>();
        if (kafkaHeaderAttr is not null)
        {
            var headerName = kafkaHeaderAttr.HeaderName ?? parameter.Name;
            return GetHeaderValue(message, headerName!);
        }

        var service = serviceProvider.GetService(parameter.ParameterType);
        if (service is not null)
            return service;

        throw new KafkaMessageDispatchException(
            $"Unable to resolve parameter of type {parameter.ParameterType.Name} {parameter.Name}"
        )
        {
            Topic = topic,
            Type = type,
        };
    }

    private static object GetHeaderValue(Message<string, byte[]> message, string headerName)
    {
        if (message.Headers == null)
            throw new KafkaMessageDispatchException($"Message headers not found");

        var header = message.Headers.FirstOrDefault(h => h.Key == headerName);
        if (header == null)
            throw new KafkaMessageDispatchException($"Header '{headerName}' not found in message");

        return System.Text.Encoding.UTF8.GetString(header.GetValueBytes());
    }
}
