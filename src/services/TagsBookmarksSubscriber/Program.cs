using KafkaConsumerApp;
using TagsBookmarksSubscriber;
using TagsBookmarksSubscriber.Setup;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddSchemaRegistry(builder.Configuration);
builder.Services.AddBookmarksClient(builder.Configuration);
var kafka = builder.AddKafkaConsumerApp(builder.Configuration);
kafka.Handle(
    "outbox.event.Bookmark",
    "bookmark_created",
    async (Bookmarks.BookmarkCreated @event, BookmarksApiClient apiClient, [FromKafkaHeader] string id) =>
        await apiClient.AddTags(@event.Bookmark.Tags, @event.Bookmark.UserId, id)
);
kafka.Handle(
    "outbox.event.Bookmark",
    "bookmark_deleted",
    async (Bookmarks.BookmarkDeleted @event, BookmarksApiClient apiClient, [FromKafkaHeader] string id) =>
        await apiClient.RemoveTags(@event.Bookmark.Tags, @event.Bookmark.UserId, id)
);
kafka.Handle(
    "outbox.event.Bookmark",
    "bookmark_updated",
    async (Bookmarks.BookmarkUpdated @event, BookmarksApiClient apiClient, [FromKafkaHeader] string id) =>
    {
        var removedTags = @event.OldBookmark.Tags.Except(@event.NewBookmark.Tags).ToArray();
        var addedTags = @event.NewBookmark.Tags.Except(@event.OldBookmark.Tags).ToArray();

        if (removedTags.Length > 0)
            await apiClient.RemoveTags(removedTags, @event.NewBookmark.UserId, id);
        if (addedTags.Length > 0)
            await apiClient.AddTags(addedTags, @event.NewBookmark.UserId, id);
    }
);
kafka.Handle(
    "outbox.event.Bookmark",
    "bookmark_archived",
    async (Bookmarks.BookmarkArchived @event, BookmarksApiClient apiClient, [FromKafkaHeader] string id) => {
        // Archiving a bookmark does not affect tag counts
    }
);
kafka.Handle(
    "outbox.event.Bookmark",
    "bookmark_restored",
    async (Bookmarks.BookmarkRestored @event, BookmarksApiClient apiClient, [FromKafkaHeader] string id) => {
        // Restoring a bookmark does not affect tag counts
    }
);
kafka.Handle(
    "outbox.event.Bookmark",
    "bookmark_permanently_deleted",
    async (Bookmarks.BookmarkPermanentlyDeleted @event, BookmarksApiClient apiClient, [FromKafkaHeader] string id) =>
        await apiClient.RemoveTags(@event.Bookmark.Tags, @event.Bookmark.UserId, id)
);

var app = builder.Build();
app.MapGet("/", () => "TagsBookmarksSubscriber is running");
app.MapDefaultEndpoints();
app.Run();
