using Bookmarks;
using BookmarksApi.Outbox;
using Confluent.Kafka;
using Confluent.SchemaRegistry.Serdes;
using Google.Protobuf.WellKnownTypes;

namespace BookmarksApi.Bookmarks;

public class BookmarkOutboxMessageMapper(
    ProtobufSerializer<BookmarkCreated> createdSerializer,
    ProtobufSerializer<BookmarkDeleted> deletedSerializer,
    ProtobufSerializer<BookmarkUpdated> updatedSerializer,
    ProtobufSerializer<BookmarkArchived> archivedSerializer,
    ProtobufSerializer<BookmarkRestored> restoredSerializer,
    ProtobufSerializer<BookmarkPermanentlyDeleted> permanentlyDeletedSerializer
)
{
    public async Task<OutboxMessage> CreateBookmarkCreatedMessageAsync(Bookmark bookmark) =>
        new(
            Id: Guid.CreateVersion7(),
            AggregateType: "Bookmark",
            AggregateId: bookmark.Id.ToString(),
            UserId: bookmark.UserId,
            Type: "bookmark_created",
            Payload: await createdSerializer.SerializeAsync(
                new BookmarkCreated
                {
                    Bookmark = new global::Bookmarks.Bookmark
                    {
                        Id = bookmark.Id,
                        Title = bookmark.Title,
                        Url = bookmark.Url,
                        Summary = bookmark.Summary ?? "",
                        CreatedAt = bookmark.CreatedAt.ToTimestamp(),
                        UpdatedAt = bookmark.UpdatedAt.ToTimestamp(),
                        Tags = { bookmark.Tags },
                        UserId = bookmark.UserId,
                    },
                },
                new SerializationContext(MessageComponentType.Value, "bookmark-created")
            )
        );

    public async Task<OutboxMessage> CreateBookmarkDeletedMessageAsync(Bookmark bookmark) =>
        new(
            Id: Guid.CreateVersion7(),
            AggregateType: "Bookmark",
            AggregateId: bookmark.Id.ToString(),
            UserId: bookmark.UserId,
            Type: "bookmark_deleted",
            Payload: await deletedSerializer.SerializeAsync(
                new BookmarkDeleted
                {
                    Bookmark = new global::Bookmarks.Bookmark
                    {
                        Id = bookmark.Id,
                        Title = bookmark.Title,
                        Url = bookmark.Url,
                        Summary = bookmark.Summary ?? "",
                        CreatedAt = bookmark.CreatedAt.ToTimestamp(),
                        UpdatedAt = bookmark.UpdatedAt.ToTimestamp(),
                        Tags = { bookmark.Tags },
                        UserId = bookmark.UserId,
                    },
                },
                new SerializationContext(MessageComponentType.Value, "bookmark-deleted")
            )
        );

    public async Task<OutboxMessage> CreateBookmarkUpdatedMessageAsync(Bookmark oldBookmark, Bookmark newBookmark) =>
        new(
            Id: Guid.CreateVersion7(),
            AggregateType: "Bookmark",
            AggregateId: newBookmark.Id.ToString(),
            UserId: newBookmark.UserId,
            Type: "bookmark_updated",
            Payload: await updatedSerializer.SerializeAsync(
                new BookmarkUpdated
                {
                    OldBookmark = new global::Bookmarks.Bookmark
                    {
                        Id = oldBookmark.Id,
                        Title = oldBookmark.Title,
                        Url = oldBookmark.Url,
                        Summary = oldBookmark.Summary ?? "",
                        CreatedAt = oldBookmark.CreatedAt.ToTimestamp(),
                        UpdatedAt = oldBookmark.UpdatedAt.ToTimestamp(),
                        Tags = { oldBookmark.Tags },
                        UserId = oldBookmark.UserId,
                        ArchivedAt = oldBookmark.ArchivedAt?.ToTimestamp(),
                    },
                    NewBookmark = new global::Bookmarks.Bookmark
                    {
                        Id = newBookmark.Id,
                        Title = newBookmark.Title,
                        Url = newBookmark.Url,
                        Summary = newBookmark.Summary ?? "",
                        CreatedAt = newBookmark.CreatedAt.ToTimestamp(),
                        UpdatedAt = newBookmark.UpdatedAt.ToTimestamp(),
                        Tags = { newBookmark.Tags },
                        UserId = newBookmark.UserId,
                        ArchivedAt = newBookmark.ArchivedAt?.ToTimestamp(),
                    },
                },
                new SerializationContext(MessageComponentType.Value, "bookmark-updated")
            )
        );

    public async Task<OutboxMessage> CreateBookmarkArchivedMessageAsync(Bookmark bookmark) =>
        new(
            Id: Guid.CreateVersion7(),
            AggregateType: "Bookmark",
            AggregateId: bookmark.Id.ToString(),
            UserId: bookmark.UserId,
            Type: "bookmark_archived",
            Payload: await archivedSerializer.SerializeAsync(
                new BookmarkArchived
                {
                    Bookmark = new global::Bookmarks.Bookmark
                    {
                        Id = bookmark.Id,
                        Title = bookmark.Title,
                        Url = bookmark.Url,
                        Summary = bookmark.Summary ?? "",
                        CreatedAt = bookmark.CreatedAt.ToTimestamp(),
                        UpdatedAt = bookmark.UpdatedAt.ToTimestamp(),
                        Tags = { bookmark.Tags },
                        UserId = bookmark.UserId,
                        ArchivedAt = bookmark.ArchivedAt?.ToTimestamp(),
                    },
                },
                new SerializationContext(MessageComponentType.Value, "bookmark-archived")
            )
        );

    public async Task<OutboxMessage> CreateBookmarkRestoredMessageAsync(Bookmark bookmark) =>
        new(
            Id: Guid.CreateVersion7(),
            AggregateType: "Bookmark",
            AggregateId: bookmark.Id.ToString(),
            UserId: bookmark.UserId,
            Type: "bookmark_restored",
            Payload: await restoredSerializer.SerializeAsync(
                new BookmarkRestored
                {
                    Bookmark = new global::Bookmarks.Bookmark
                    {
                        Id = bookmark.Id,
                        Title = bookmark.Title,
                        Url = bookmark.Url,
                        Summary = bookmark.Summary ?? "",
                        CreatedAt = bookmark.CreatedAt.ToTimestamp(),
                        UpdatedAt = bookmark.UpdatedAt.ToTimestamp(),
                        Tags = { bookmark.Tags },
                        UserId = bookmark.UserId,
                        ArchivedAt = bookmark.ArchivedAt?.ToTimestamp(),
                    },
                },
                new SerializationContext(MessageComponentType.Value, "bookmark-restored")
            )
        );

    public async Task<OutboxMessage> CreateBookmarkPermanentlyDeletedMessageAsync(Bookmark bookmark) =>
        new(
            Id: Guid.CreateVersion7(),
            AggregateType: "Bookmark",
            AggregateId: bookmark.Id.ToString(),
            UserId: bookmark.UserId,
            Type: "bookmark_permanently_deleted",
            Payload: await permanentlyDeletedSerializer.SerializeAsync(
                new BookmarkPermanentlyDeleted
                {
                    Bookmark = new global::Bookmarks.Bookmark
                    {
                        Id = bookmark.Id,
                        Title = bookmark.Title,
                        Url = bookmark.Url,
                        Summary = bookmark.Summary ?? "",
                        CreatedAt = bookmark.CreatedAt.ToTimestamp(),
                        UpdatedAt = bookmark.UpdatedAt.ToTimestamp(),
                        Tags = { bookmark.Tags },
                        UserId = bookmark.UserId,
                        ArchivedAt = bookmark.ArchivedAt?.ToTimestamp(),
                    },
                },
                new SerializationContext(MessageComponentType.Value, "bookmark-permanently-deleted")
            )
        );
}
