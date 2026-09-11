namespace Gateway.HtmlTransform;

public static class ReverseProxyBuilderExtensions
{
    public static IReverseProxyBuilder AddHtmlTransform(
        this IReverseProxyBuilder builder,
        Action<IServiceCollection> configureMutations
    )
    {
        configureMutations(builder.Services);
        return builder.AddTransformFactory<HtmlTransformFactory>();
    }
}
