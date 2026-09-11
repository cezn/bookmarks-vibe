using Microsoft.AspNetCore.Http;

namespace Microsoft.Extensions.Hosting;

public static class HeadersEndpoint
{
    public static async Task HandleAsync(HttpContext context, HttpRequest request)
    {
        var headers = request.Headers;
        var htmlBuilder = new System.Text.StringBuilder("<html><body><h1>Request Headers</h1><ul>");
        foreach (var header in headers)
        {
            htmlBuilder.Append($"<li><strong>{header.Key}:</strong> {header.Value}</li>");
        }
        htmlBuilder.Append("</ul></body></html>");

        context.Response.ContentType = "text/html";
        await context.Response.WriteAsync(htmlBuilder.ToString());
    }
}
