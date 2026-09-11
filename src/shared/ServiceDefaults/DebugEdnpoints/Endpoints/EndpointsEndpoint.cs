using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting;

public static class EndpointsEndpoint
{
    public static async Task HandleAsync(HttpContext context, EndpointDataSource endpointDataSource)
    {
        var endpoints = endpointDataSource.Endpoints.OfType<RouteEndpoint>();

        var htmlBuilder = new System.Text.StringBuilder("<html><body><h1>Available Endpoints</h1><ul>");
        foreach (var endpoint in endpoints)
        {
            var methods = string.Join(", ", endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods ?? []);
            var displayName = endpoint.Metadata.GetMetadata<EndpointNameMetadata>()?.EndpointName ?? "Unnamed";
            htmlBuilder.Append(
                $"""
                <li>
                    <strong>{methods}</strong> {endpoint.RoutePattern.RawText} - {displayName}
                </li>
                """
            );
        }
        htmlBuilder.Append("</ul></body></html>");

        context.Response.ContentType = "text/html";
        await context.Response.WriteAsync(htmlBuilder.ToString());
    }
}
