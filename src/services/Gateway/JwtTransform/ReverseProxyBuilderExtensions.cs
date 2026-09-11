namespace Gateway.JwtTransform;

public static class ReverseProxyBuilderExtensions
{
    public static IReverseProxyBuilder AddJwtTransform(this IReverseProxyBuilder builder, IConfiguration config)
    {
        builder.Services.AddOptions<JwtOptions>().Bind(config).ValidateDataAnnotations().ValidateOnStart();
        builder.Services.AddSingleton<JwtTokenService>();
        builder.AddTransformFactory<JwtTransformFactory>();

        return builder;
    }
}
