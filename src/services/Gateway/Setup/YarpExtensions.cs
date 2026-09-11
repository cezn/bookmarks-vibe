using Gateway.HtmlTransform;
using Gateway.HtmlTransform.Mutations;
using Gateway.JwtTransform;

namespace Gateway.Setup;

public static class YarpExtensions
{
    public static IServiceCollection AddYarp(this IServiceCollection services, IConfiguration config)
    {
        services
            .AddOpenTelemetry()
            .WithTracing(x => x.AddSource("Yarp.ReverseProxy"))
            .WithMetrics(x => x.AddMeter("Yarp.ReverseProxy"));

        services
            .AddReverseProxy()
            .AddHttpProxySupport()
            .LoadFromConfig(config.GetSection("ReverseProxy"))
            .AddJwtTransform(config.GetSection("Jwt"))
            .AddHtmlTransform(servicesCollection =>
            {
                servicesCollection.AddSingleton<IHtmlMutation, TraceparentMutation>();
                servicesCollection.AddSingleton<IHtmlMutation, CspNonceMutation>();
            });

        return services;
    }
}
