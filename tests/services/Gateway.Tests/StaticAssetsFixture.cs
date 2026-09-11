using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Gateway.Tests;

internal sealed class StaticAssetsFixture
{
    private readonly WireMockServer _server;

    public StaticAssetsFixture() => _server = WireMockServer.Start();

    public string Url => _server.Url ?? throw new InvalidOperationException("WireMockServer URL is null");

    public void ExpectGetIndex(string response, int statusCode = 200) =>
        _server
            .Given(Request.Create().WithPath("/").UsingGet())
            .RespondWith(
                Response.Create().WithStatusCode(statusCode).WithHeader("Content-Type", "text/plain").WithBody(response)
            );
}
