using Yarp.ReverseProxy.Transforms.Builder;

namespace Gateway.HtmlTransform;

public sealed class HtmlTransformFactory : ITransformFactory
{
    internal const string Key = "HtmlTransform";

    public bool Build(TransformBuilderContext context, IReadOnlyDictionary<string, string> transformValues)
    {
        if (transformValues.ContainsKey(Key))
            context.ResponseTransforms.Add(
                new HtmlTransform(
                    context.Services.GetRequiredService<IEnumerable<IHtmlMutation>>(),
                    context.Services.GetRequiredService<ILogger<HtmlTransform>>()
                )
            );

        return transformValues.ContainsKey(Key);
    }

    public bool Validate(
        TransformRouteValidationContext context,
        IReadOnlyDictionary<string, string> transformValues
    ) => transformValues.ContainsKey(Key);
}
