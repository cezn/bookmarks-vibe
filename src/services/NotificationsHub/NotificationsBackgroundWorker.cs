using Microsoft.AspNetCore.SignalR;

namespace NotificationsHub;

public sealed class NotificationsBackgroundWorker(
    IHubContext<NotificationsHub, INotificationsClient> hubContext,
    ILogger<NotificationsBackgroundWorker> logger,
    UserDelayStorage userDelayStorage
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var random = new Random();
        while (!stoppingToken.IsCancellationRequested)
        {
            var message = $"Notification {random.Next(1, 100)}";
            logger.LogInformation("Sending notification: {Message}", message);
            await hubContext.Clients.All.Publish(new Notification(message));

            // Use a default delay when no users are connected, otherwise use first user's delay
            var userDelays = userDelayStorage.GetAllUserDelays();
            var delaySeconds = userDelays.Count > 0 ? userDelays.Values.First() : 5;

            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
        }
    }
}
