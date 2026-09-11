using Microsoft.Extensions.AI;
using OllamaSharp;
using Polly;
using Polly.CircuitBreaker;

namespace SummarizeApi.Setup;

public static class OllamaExtensions
{
    public static IHostApplicationBuilder AddCustomOllama(this IHostApplicationBuilder builder)
    {
        builder.Services.AddKeyedSingleton<CircuitBreakerStateProvider>("OllamaHttpClient");
        builder
            .Services.AddHttpClient(
                "OllamaHttpClient",
                client =>
                {
                    client.BaseAddress = new Uri(
                        builder.Configuration["Ollama:Url"]
                            ?? throw new InvalidOperationException("Ollama:Url configuration is required.")
                    );
                }
            )
            .AddResilienceHandler(
                "OllamaHttpClient",
                (pipeline, ctx) =>
                {
                    var opts =
                        builder
                            .Configuration.GetSection("Ollama:Resilience:CircuitBreaker")
                            .Get<CircuitBreakerStrategyOptions<HttpResponseMessage>>()
                        ?? throw new InvalidOperationException(
                            "Ollama:Resilience:CircuitBreaker configuration is required."
                        );
                    opts.StateProvider = ctx.ServiceProvider.GetRequiredKeyedService<CircuitBreakerStateProvider>(
                        "OllamaHttpClient"
                    );
                    pipeline.AddCircuitBreaker(opts);
                }
            );
        builder.Services.AddSingleton(sp => new OllamaApiClient(
            sp.GetRequiredService<IHttpClientFactory>().CreateClient("OllamaHttpClient"),
            defaultModel: "llama3.2"
        ));
        builder
            .Services.AddChatClient(sp => sp.GetRequiredService<OllamaApiClient>())
            .UseOpenTelemetry(sourceName: builder.Environment.ApplicationName);

        return builder;
    }
}
