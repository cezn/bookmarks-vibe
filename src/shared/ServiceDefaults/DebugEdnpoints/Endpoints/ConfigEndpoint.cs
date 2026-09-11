using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Hosting;

public static class ConfigEndpoint
{
    public static IResult Handle(IConfiguration config)
    {
        return TypedResults.Text(((IConfigurationRoot)config).GetDebugView(), "text/plain; charset=utf-8");
    }
}
