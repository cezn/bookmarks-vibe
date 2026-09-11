using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Gateway.Tests;

[TestClass]
public sealed class Test1
{
    private static BookmarksFixture s_bookmarksFixture = default!;
    private static StaticAssetsFixture s_staticAssetsFixture = default!;

    [AssemblyInitialize]
    public static void AssemblyInit(TestContext context)
    {
        Environment.SetEnvironmentVariable("OTEL_SERVICE_NAME", "BookmarksApi.Tests");
        s_bookmarksFixture = new BookmarksFixture();
        s_staticAssetsFixture = new StaticAssetsFixture();
    }

    [TestMethod]
    public async Task Should_Redirect_To_Login_When_Requesting_Assets_Without_Cookie()
    {
        using var http = CreateAppFactory().CreateClient();
        var res = await http.GetAsync("/");
        Assert.AreEqual(HttpStatusCode.Redirect, res.StatusCode);
    }

    [TestMethod]
    public async Task Should_Redirect_When_Requesting_Assets_With_Cookie()
    {
        using var http = CreateAppFactory().CreateClient();
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/bookmarks")
        {
            Headers = { { "Cookie", "bookmarks-auth=invalid" } },
        };
        var res = await http.SendAsync(req);
        Assert.AreEqual("", await res.Content.ReadAsStringAsync());
        Assert.AreEqual(HttpStatusCode.Redirect, res.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return_Ok_When_Requesting_Assets_With_Cookie()
    {
        s_staticAssetsFixture.ExpectGetIndex("hello world", 200);
        using var http = CreateAuthenticatedClient();
        var res = await http.GetAsync("/");
        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
        Assert.AreEqual("hello world", await res.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public async Task Should_Redirect_To_Login_When_Requesting_Bookmakrs_Without_Cookie()
    {
        using var http = CreateAppFactory().CreateClient();
        var res = await http.GetAsync("/api/bookmarks");
        Assert.AreEqual(HttpStatusCode.Redirect, res.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateAppFactory()
    {
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(x =>
            x.ConfigureAppConfiguration(
                (_, config) =>
                    config.AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["Logging:LogLevel:Default"] = "Trace",
                            ["Logging:LogLevel:Microsoft.AspNetCore"] = "Trace",

                            ["ReverseProxy:Clusters:bookmarks:Destinations:destination1:Address"] =
                                s_bookmarksFixture.Url,
                            ["ReverseProxy:Clusters:static-assets:Destinations:destination1:Address"] =
                                s_staticAssetsFixture.Url,
                        }
                    )
            )
        );

        factory.ClientOptions.AllowAutoRedirect = false;

        return factory;
    }

    private static HttpClient CreateAuthenticatedClient()
    {
        var factory = CreateAppFactory();
        factory.Services.GetRequiredService<ILogger<Test1>>().LogInformation("Creating authenticated client");
        var cookieValue = CreateCoookie(factory);
        var http = factory.CreateClient();
        http.DefaultRequestHeaders.Add("Cookie", $"bookmarks-auth={cookieValue}");
        return http;
    }

    private static string CreateCoookie(WebApplicationFactory<Program> factory)
    {
        var ticket = new AuthenticationTicket(
            new ClaimsPrincipal(
                new ClaimsIdentity(
                    [new Claim(ClaimTypes.Name, "testuser"), new Claim(ClaimTypes.Email, "test@cezn.tech")],
                    "Identity.Application"
                )
            ),
            "Identity.Application"
        );
        var ticketBytes = new TicketSerializer().Serialize(ticket);
        var cookieValueBytes = factory
            .Services.GetRequiredService<IDataProtectionProvider>()
            .CreateProtector([
                "Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationMiddleware",
                "Identity.Application",
                "v2",
            ])
            .Protect(ticketBytes);
        var cookieValue = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(cookieValueBytes);

        return cookieValue;
    }
}
