namespace Gateway.HtmlTransform;

/// <summary>
/// Represents a single, independent HTML mutation.
/// Implementations modify <see cref="HtmlMutationContext.Html"/> in place.
/// </summary>
public interface IHtmlMutation
{
    /// <summary>Apply this mutation to the HTML content.</summary>
    void Apply(HtmlMutationContext context);
}
