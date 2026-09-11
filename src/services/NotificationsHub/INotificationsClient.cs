public interface INotificationsClient
{
    Task Publish(Notification notification);
}
