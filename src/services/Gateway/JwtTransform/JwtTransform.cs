using System.Net.Http.Headers;

using Yarp.ReverseProxy.Transforms;

namespace Gateway.JwtTransform;

public class JwtTransform(JwtTokenService jwtTokenService) : RequestTransform
{
    public override ValueTask ApplyAsync(RequestTransformContext context)
    {
        if (context.HttpContext.User is { Identity.IsAuthenticated: true } user)
            context.ProxyRequest.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                jwtTokenService.GenerateToken(user)
            );

        return ValueTask.CompletedTask;
    }
}
