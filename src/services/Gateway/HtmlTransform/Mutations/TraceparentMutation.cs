using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Gateway.HtmlTransform.Mutations;

/// <summary>
/// Injects a &lt;meta name="traceparent"&gt; tag into &lt;head&gt; for distributed tracing.
/// </summary>
public partial class TraceparentMutation : IHtmlMutation
{
    [GeneratedRegex(@"<head[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex HeadRegex();

    public void Apply(HtmlMutationContext context)
    {
        // Walk up to root Activity
        var activity = Activity.Current;
        while (activity?.Parent != null)
            activity = activity.Parent;

        if (activity == null || string.IsNullOrEmpty(activity.Id))
            return;

        var metaTag = $"<meta name=\"traceparent\" content=\"{activity.Id}\" />";
        context.Html = HeadRegex().Replace(context.Html, match => match.Value + Environment.NewLine + metaTag, 1);

        context.Logger.LogDebug("Inserted traceparent meta tag into HTML response.");
    }
}
