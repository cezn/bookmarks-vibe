using System.ComponentModel.DataAnnotations;

namespace BookmarksApi.Bookmarks;

public class SchemaRegistryOptions
{
    [Required]
    public string Url { get; set; } = null!;
}
