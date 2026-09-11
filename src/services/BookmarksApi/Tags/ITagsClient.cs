namespace BookmarksApi.Tags;

public interface ITagsClient
{
    Task OnTagUpdated(Tag tag);
}
