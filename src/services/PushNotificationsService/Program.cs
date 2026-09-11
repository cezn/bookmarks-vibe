using PushNotificationsService.Setup;
using PushNotificationsService.Subscriptions;
using ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.Configure<HostOptions>(opt =>
{
    opt.ServicesStartConcurrently = true;
    opt.ServicesStopConcurrently = true;
    opt.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.StopHost;
});
builder.Services.Configure<RouteHandlerOptions>(x => x.ThrowOnBadRequest = true);
builder.Services.AddOpenApi();
builder.Services.AddCustomOpenApi(builder.Configuration);
builder.Services.AddCustomAuth(builder.Configuration.GetSection("Jwt"));
builder.Services.AddNpgsql(builder.Configuration.GetConnectionString("PushNotifications"), builder.Environment);

var app = builder.Build();
app.UseRouting();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapPushNotificationEndpoints();
app.MapOpenApi();
app.MapDefaultEndpoints();
app.Run();
