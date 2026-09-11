using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace BookmarksApi.Setup;

public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public Dictionary<string, string> ConsumerApiKeys { get; set; } = new();
}

public class ApiKeyAuthenticationHandler(
    IOptionsMonitor<ApiKeyAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder
) : AuthenticationHandler<ApiKeyAuthenticationOptions>(options, logger, encoder)
{
    private const string ApiKeyHeaderName = "X-API-Key";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyHeaderName, out StringValues value))
            return Task.FromResult(AuthenticateResult.NoResult());

        try
        {
            var apiKey = value.ToString();
            if (string.IsNullOrWhiteSpace(apiKey))
                return Task.FromResult(AuthenticateResult.Fail("API Key is empty"));

            var consumerName = Options.ConsumerApiKeys.FirstOrDefault(kvp => kvp.Value == apiKey).Key;

            if (string.IsNullOrEmpty(consumerName))
                return Task.FromResult(AuthenticateResult.Fail("Invalid API Key"));

            var claims = new[] { new Claim(ClaimTypes.Name, consumerName), new Claim("consumer", consumerName) };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
        catch (Exception ex)
        {
            return Task.FromResult(AuthenticateResult.Fail($"Authentication failed: {ex.Message}"));
        }
    }
}
