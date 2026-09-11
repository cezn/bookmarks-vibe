using Microsoft.AspNetCore.SignalR;

namespace NotificationsHub;

public sealed class NotificationsHub(UserDelayStorage userDelayStorage) : Hub<INotificationsClient>
{
    public Task SetDelay(int delay)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(delay, 1);

        userDelayStorage.SetDelay(
            Context.UserIdentifier ?? throw new InvalidOperationException("Missing user identifier"),
            delay
        );
        return Task.CompletedTask;
    }

    public int GetDelay() =>
        userDelayStorage.GetDelay(
            Context.UserIdentifier ?? throw new InvalidOperationException("Missing user identifier")
        );
}
