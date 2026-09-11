using System.Text;
using Yarp.ReverseProxy.Transforms;

namespace Gateway.HtmlTransform;

/// <summary>
/// YARP response transform that reads the HTML body once, runs all registered
/// <see cref="IHtmlMutation"/> instances in order, then writes the result back.
/// </summary>
public sealed class HtmlTransform(IEnumerable<IHtmlMutation> mutations, ILogger<HtmlTransform> logger)
    : ResponseTransform
{
    private readonly IHtmlMutation[] _mutations = mutations.ToArray();

    public override async ValueTask ApplyAsync(ResponseTransformContext context)
    {
        // Only process HTML responses
        if (
            context.ProxyResponse?.Content?.Headers?.ContentType?.MediaType?.StartsWith(
                "text/html",
                StringComparison.OrdinalIgnoreCase
            ) != true
        )
            return;

        // Read the body once
        var html = await context.ProxyResponse!.Content.ReadAsStringAsync();
        if (string.IsNullOrEmpty(html))
            return;

        // Run each mutation in order
        var mutationContext = new HtmlMutationContext(html, context.HttpContext, logger);

        foreach (var mutation in _mutations)
        {
            mutation.Apply(mutationContext);
        }

        // Suppress YARP's default body copy and write the modified HTML
        context.SuppressResponseBody = true;

        var bytes = Encoding.UTF8.GetBytes(mutationContext.Html);
        context.HttpContext.Response.ContentLength = bytes.Length;

        await context.HttpContext.Response.Body.WriteAsync(bytes);
    }
}
