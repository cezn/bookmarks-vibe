using NotificationsHub;
using NotificationsHub.Setup;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddCustomAuth(builder.Configuration.GetSection("Jwt"));
builder.Services.AddSignalR();
builder.Services.AddHostedService<NotificationsBackgroundWorker>();
builder.Services.AddSingleton<UserDelayStorage>();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/notifications", () => "Hello World!");
app.MapHub<NotificationsHub.NotificationsHub>("/notifications/noti-hub");
app.MapDefaultEndpoints();
app.Run();
