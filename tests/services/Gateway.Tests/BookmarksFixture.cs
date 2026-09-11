using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Gateway.Tests;

internal sealed class BookmarksFixture
{
    private readonly WireMockServer _server;

    public BookmarksFixture() => _server = WireMockServer.Start();

    public string Url => _server.Url ?? throw new InvalidOperationException("WireMockServer URL is null");

    public void ExpectGetBookmarks() =>
        _server
            .Given(Request.Create().WithPath("/api/bookmarks").UsingGet())
            .RespondWith(
                Response.Create().WithStatusCode(200).WithHeader("Content-Type", "text/plain").WithBody("Hello world!")
            );
}
