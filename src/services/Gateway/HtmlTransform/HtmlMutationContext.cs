using Microsoft.AspNetCore.Http;

namespace Gateway.HtmlTransform;

/// <summary>
/// Context passed to each HTML mutation. Mutations modify <see cref="Html"/> in place
/// and may optionally set response headers via <see cref="HttpContext"/>.
/// </summary>
public sealed class HtmlMutationContext
{
    public HtmlMutationContext(string html, HttpContext httpContext, ILogger logger)
    {
        Html = html;
        HttpContext = httpContext;
        Logger = logger;
    }

    /// <summary>The current HTML content — mutations should modify this property.</summary>
    public string Html { get; internal set; }

    /// <summary>Access to the HTTP response for setting headers.</summary>
    public HttpContext HttpContext { get; }

    /// <summary>Logger for diagnostic output.</summary>
    public ILogger Logger { get; }
}
