using System.Text.Json.Serialization;
using ConsoleAppFramework;

var app = ConsoleApp.Create();
app.Add("seed", SeedCommand.HandleSeed);
app.Add("import-edge", ImportEdgeCommand.HandleImportEdge);

app.Run(args);

public class BookmarkPayload
{
    public required string Title { get; set; }
    public required string Url { get; set; }
    public required List<string> Tags { get; set; }
}

[JsonSerializable(typeof(BookmarkPayload))]
[JsonSerializable(typeof(BookmarksFile))]
public partial class BookmarkPayloadJsonContext : JsonSerializerContext { }
