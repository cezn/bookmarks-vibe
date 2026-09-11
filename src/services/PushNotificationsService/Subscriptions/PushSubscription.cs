using System.ComponentModel.DataAnnotations;

namespace PushNotificationsService.Subscriptions;

public record PushSubscription(
    int Id,
    string UserId,
    [Required] string DeviceToken,
    [Required] string Platform,
    string? Endpoint,
    string? Auth,
    string? P256dh,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    bool IsActive
);

public record CreateSubscriptionRequest(
    [Required] string DeviceToken,
    [Required] string Platform,
    string? Endpoint,
    string? Auth,
    string? P256dh
);

public record PushNotification(
    int Id,
    string UserId,
    [Required] string Title,
    [Required] string Body,
    Dictionary<string, string>? Data,
    DateTimeOffset CreatedAt,
    bool Sent
);

public record SendNotificationRequest(
    string? UserId,
    [Required] string Title,
    [Required] string Body,
    Dictionary<string, string>? Data
);
