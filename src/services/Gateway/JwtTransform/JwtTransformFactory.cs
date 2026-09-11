using Yarp.ReverseProxy.Transforms.Builder;

namespace Gateway.JwtTransform;

public class JwtTransformFactory : ITransformFactory
{
    internal const string Key = "JwtTransform";

    public bool Build(TransformBuilderContext context, IReadOnlyDictionary<string, string> transformValues)
    {
        if (transformValues.ContainsKey(Key))
            context.RequestTransforms.Add(new JwtTransform(context.Services.GetRequiredService<JwtTokenService>()));

        return transformValues.ContainsKey(Key);
    }

    public bool Validate(
        TransformRouteValidationContext context,
        IReadOnlyDictionary<string, string> transformValues
    ) => transformValues.ContainsKey(Key);
}
