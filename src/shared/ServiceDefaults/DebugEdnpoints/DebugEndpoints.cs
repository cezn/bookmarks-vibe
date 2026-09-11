using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Extensions.Hosting;

public static class DebugEndpoints
{
    public static WebApplication MapDebugEndpoints(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
            return app;

        var debug = app.MapGroup("");
        debug.WithTags("Debug");
        debug
            .MapGet("/_endpoints", EndpointsEndpoint.HandleAsync)
            .AddOpenApiOperationTransformer(
                (operation, context, ct) =>
                {
                    operation.Summary = "List all registered endpoints";
                    operation.Description = "Returns an HTML page listing all endpoints in the application.";
                    return Task.CompletedTask;
                }
            )
            .AllowAnonymous();
        debug
            .MapGet("/_headers", HeadersEndpoint.HandleAsync)
            .AddOpenApiOperationTransformer(
                async (operation, context, ct) =>
                {
                    operation.Summary = "Display request headers";
                    operation.Description = "Returns an HTML page showing all headers from the current request.";
                }
            )
            .AllowAnonymous();
        debug
            .MapGet("/_config", ConfigEndpoint.Handle)
            .AddOpenApiOperationTransformer(
                async (operation, context, ct) =>
                {
                    operation.Summary = "Show application configuration";
                    operation.Description = "Returns the current configuration values as a debug view.";
                }
            )
            .AllowAnonymous();
        debug
            .MapGet("/_services", ServicesEndpoint.HandleAsync)
            .AddOpenApiOperationTransformer(
                async (operation, context, ct) =>
                {
                    operation.Summary = "List registered services";
                    operation.Description = "Returns an HTML page showing all registered service descriptors.";
                }
            )
            .AllowAnonymous();

        return app;
    }
}
