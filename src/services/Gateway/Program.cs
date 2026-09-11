using System.Diagnostics;
using System.Security.Claims;
using Gateway.Setup;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);
OverrideYarpActivity(builder.Environment);
builder.AddServiceDefaults();
builder.Services.AddProblemDetails();
builder.Services.AddCustomRateLimiter(builder.Configuration);
builder.AddRedisClient("redis");
builder.Services.AddCustomAuth(builder.Configuration.GetConnectionString("Redis"));
builder.Services.AddYarp(builder.Configuration);

var app = builder.Build();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/me", AuthEndpoints.Me).RequireAuthorization();
app.MapPost("/logout", AuthEndpoints.Logout);
app.MapReverseProxy();
app.MapDefaultEndpoints();
app.Run();

static void OverrideYarpActivity(IWebHostEnvironment environment)
{
    if (!environment.IsDevelopment())
        return;

    // Override YARP's span name,
    // https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.AspNetCore/Implementation/HttpInListener.cs#L270
    // https://github.com/dotnet/yarp/issues/2667
    var listener = new ActivityListener
    {
        ShouldListenTo = activitySource => activitySource.Name == "Microsoft.AspNetCore",
        ActivityStopped = activity =>
        {
            if (activity.OperationName == "Microsoft.AspNetCore.Hosting.HttpRequestIn")
                activity.DisplayName =
                    $"{activity.GetTagItem("http.request.method")} {activity.GetTagItem("url.path")}";
        },
    };
    ActivitySource.AddActivityListener(listener);
}

public static class AuthEndpoints
{
    public record UserInfoResponse(string? Name, string? Email, string? Id);

    public static UserInfoResponse Me(HttpContext ctx) =>
        new(
            ctx.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value,
            ctx.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value,
            ctx.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value
        );

    public static async Task Logout(HttpContext ctx) => await ctx.SignOutAsync("Identity.Application");
}
