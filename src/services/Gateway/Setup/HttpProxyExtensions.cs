using System.Net;

namespace Gateway.Setup;

public static class HttpProxyExtensions
{
    public static IReverseProxyBuilder AddHttpProxySupport(this IReverseProxyBuilder builder)
    {
        var httpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
        var httpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");

        if (string.IsNullOrEmpty(httpProxy) && string.IsNullOrEmpty(httpsProxy))
            return builder;

        return builder.ConfigureHttpClient(
            (context, handler) =>
            {
                handler.UseProxy = true;
                handler.Proxy = new WebProxy(httpProxy ?? httpsProxy) { BypassProxyOnLocal = false };
            }
        );
    }
}
