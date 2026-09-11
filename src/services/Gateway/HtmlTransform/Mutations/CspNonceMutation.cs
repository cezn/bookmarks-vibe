using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Gateway.HtmlTransform.Mutations;

/// <summary>
/// Generates a per-request CSP nonce and injects it into every &lt;script&gt; tag,
/// then sets the Content-Security-Policy header.
/// </summary>
public partial class CspNonceMutation : IHtmlMutation
{
    [GeneratedRegex(@"<script\b[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex ScriptTagRegex();

    public void Apply(HtmlMutationContext context)
    {
        var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        // TODO: Verify if it isn't broken since it also adds nonce to script tags inside malicious payuload.
        context.Html = ScriptTagRegex()
            .Replace(
                context.Html,
                match =>
                {
                    var tag = match.Value;
                    return tag.Insert(tag.Length - 1, " nonce=\"" + nonce + "\"");
                }
            );

        context.HttpContext.Response.Headers.Append("Content-Security-Policy", $"script-src 'self' 'nonce-{nonce}'");

        context.Logger.LogDebug("Injected CSP nonce into HTML response: {Nonce}", nonce);
    }
}
