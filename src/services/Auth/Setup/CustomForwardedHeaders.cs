using System.Net;

namespace Auth.Setup;

public static class CustomForwardedHeadersExtensions
{
    public static void UseCustomForwardedHeaders(this IApplicationBuilder app, IConfiguration config)
    {
        var options = new ForwardedHeadersOptions()
        {
            ForwardedHeaders = config.GetValue<Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders>("ForwardedHeaders"),
        };

        foreach (
            var ipNetwork in config
                .GetSection("KnownIPNetworks")
                .GetChildren()
                .Select(x => IPNetwork.Parse(x.Get<string>()!))
        )
            options.KnownIPNetworks.Add(ipNetwork);

        if (options.KnownIPNetworks.Count == 0)
            throw new InvalidOperationException("No KnownIPNetworks configured for Forwarded Headers Middleware.");

        app.UseForwardedHeaders(options);
    }
}
