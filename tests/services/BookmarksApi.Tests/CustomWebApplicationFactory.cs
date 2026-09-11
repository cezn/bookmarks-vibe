using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BookmarksApi.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder
            .ConfigureAppConfiguration(
                (context, configBuilder) =>
                {
                    var inMemorySettings = new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:BookmarksRw"] =
                            "host=127.0.0.1;database=bookmarksdb;user id=postgres;password=secret;",
                        ["ConnectionStrings:BookmarksRo"] =
                            "host=127.0.0.1;database=bookmarksdb;user id=postgres;password=secret;",
                        ["Bookmarks:SchemaRegistry:Url"] = "http://127.0.0.1:8081",
                        ["Kafka:BootstrapServers"] = "127.0.0.1:9092",
                        ["Kafka:GroupId"] = "bookmark-tags-extractor",
                        ["Jwt:Key"] = "930591a8-2c0d-5b91-ba76-60087bd989c91c167c5c-9192-5b98-aaa2-f27d9cc473c7",
                        ["Jwt:Audience"] = "bookmarks-api",
                        ["Jwt:Issuer"] = "bookmarks-gateway",
                        ["Jwt:ServiceApiKeys:BookmarkApi.Tests"] = "test-apikey",
                    };
                    configBuilder.AddInMemoryCollection(inMemorySettings);
                }
            )
            .ConfigureServices(
                (_, sp) =>
                    sp.AddOpenTelemetry()
                        .WithTracing(x =>
                        {
                            x.AddSource("BookmarksApi.Tests");
                        })
            );
    }
}
