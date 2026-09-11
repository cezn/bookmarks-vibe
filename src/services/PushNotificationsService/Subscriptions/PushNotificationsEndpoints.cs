using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace PushNotificationsService.Subscriptions;

public static class PushNotificationsEndpoints
{
    private static string GetUserId(ClaimsPrincipal user) =>
        user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value
        ?? throw new InvalidOperationException("User identifier not found in claims");

    public static void MapPushNotificationEndpoints(this WebApplication app)
    {
        app.MapPost("/api/subscriptions", RegisterSubscription);
        app.MapDelete("/api/subscriptions/{id:int}", DeleteSubscription);
        app.MapGet("/api/subscriptions", GetSubscriptions);
        app.MapPost("/api/notifications", SendNotification);
        app.MapGet("/api/notifications/{id:int}/status", GetNotificationStatus);
    }

    /// <summary>
    /// Register a device or web app for push notifications
    /// </summary>
    /// <response code="201">Subscription created successfully</response>
    /// <response code="400">Invalid subscription request</response>
    [ProducesResponseType(typeof(PushSubscription), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public static async Task<IResult> RegisterSubscription(
        HttpContext httpContext,
        NpgsqlDataSource ds,
        CreateSubscriptionRequest request
    )
    {
        var userId = GetUserId(httpContext.User);

        using var con = await ds.OpenConnectionAsync();
        var subscription = await con.CreateSubscriptionAsync(userId, request);

        return Results.Created($"/api/subscriptions/{subscription.Id}", subscription);
    }

    /// <summary>
    /// Remove a device from receiving push notifications
    /// </summary>
    /// <response code="204">Subscription deleted successfully</response>
    /// <response code="404">Subscription not found</response>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public static async Task<IResult> DeleteSubscription(HttpContext httpContext, NpgsqlDataSource ds, int id)
    {
        var userId = GetUserId(httpContext.User);

        using var con = await ds.OpenConnectionAsync();
        var deleted = await con.DeleteSubscriptionAsync(id, userId);

        if (!deleted)
            return Results.NotFound();

        return Results.NoContent();
    }

    /// <summary>
    /// Get all active subscriptions for the current user
    /// </summary>
    /// <response code="200">List of subscriptions</response>
    [ProducesResponseType(typeof(IEnumerable<PushSubscription>), StatusCodes.Status200OK)]
    public static async Task<IResult> GetSubscriptions(HttpContext httpContext, NpgsqlDataSource ds)
    {
        var userId = GetUserId(httpContext.User);

        using var con = await ds.OpenConnectionAsync();
        var subscriptions = await con.GetSubscriptionsByUserIdAsync(userId);

        return Results.Ok(subscriptions);
    }

    /// <summary>
    /// Send a push notification to a user
    /// </summary>
    /// <response code="202">Notification accepted for delivery</response>
    /// <response code="400">Invalid notification request</response>
    [ProducesResponseType(typeof(PushNotification), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public static async Task<IResult> SendNotification(
        HttpContext httpContext,
        NpgsqlDataSource ds,
        SendNotificationRequest request
    )
    {
        var userId = request.UserId ?? GetUserId(httpContext.User);

        using var con = await ds.OpenConnectionAsync();
        var notification = await con.CreateNotificationAsync(userId, request);

        return Results.Accepted($"/api/notifications/{notification.Id}/status", notification);
    }

    /// <summary>
    /// Get notification delivery status
    /// </summary>
    /// <response code="200">Notification status</response>
    /// <response code="404">Notification not found</response>
    [ProducesResponseType(typeof(PushNotification), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public static async Task<IResult> GetNotificationStatus(NpgsqlDataSource ds, int id)
    {
        using var con = await ds.OpenConnectionAsync();
        var notification = await con.GetNotificationByIdAsync(id);

        if (notification == null)
            return Results.NotFound();

        return Results.Ok(notification);
    }
}
