using System.Security.Claims;
using Gateway.JwtTransform;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Yarp.ReverseProxy.Transforms;

namespace Gateway.UnitTests.JwtTransform;

[TestClass]
public class JwtTransformTests
{
    private readonly JwtOptions _jwtOptions = new()
    {
        Issuer = "test-issuer",
        Audience = "test-audience",
        Key = "super-secret-key-that-is-at-least-32-characters-long-for-hmac-sha256-algorithm",
        Lifetime = TimeSpan.FromHours(1),
    };
    private JwtTokenService _jwtTokenService = default!;
    private Gateway.JwtTransform.JwtTransform _jwtTransform = default!;

    [TestInitialize]
    public void TestInitialize()
    {
        _jwtTokenService = new JwtTokenService(Options.Create(_jwtOptions));
        _jwtTransform = new Gateway.JwtTransform.JwtTransform(_jwtTokenService);
    }

    [TestMethod]
    public async Task ApplyAsync_WhenUserIsAuthenticated_ShouldAddAuthorizationHeader()
    {
        // Arrange
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "user123") };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var principal = new ClaimsPrincipal(identity);
        var expectedToken = _jwtTokenService.GenerateToken(principal);

        var httpContext = new DefaultHttpContext { User = principal };
        var proxyRequest = new HttpRequestMessage();
        var context = new RequestTransformContext { HttpContext = httpContext, ProxyRequest = proxyRequest };

        // Act
        await _jwtTransform.ApplyAsync(context);

        // Assert
        Assert.IsNotNull(proxyRequest.Headers.Authorization);
        Assert.AreEqual("Bearer", proxyRequest.Headers.Authorization.Scheme);
        Assert.AreEqual(expectedToken, proxyRequest.Headers.Authorization.Parameter);
    }

    [TestMethod]
    public async Task ApplyAsync_WhenUserIsNotAuthenticated_ShouldNotAddAuthorizationHeader()
    {
        // Arrange
        var identity = new ClaimsIdentity(); // Not authenticated
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        var proxyRequest = new HttpRequestMessage();
        var context = new RequestTransformContext { HttpContext = httpContext, ProxyRequest = proxyRequest };

        // Act
        await _jwtTransform.ApplyAsync(context);

        // Assert
        Assert.IsNull(proxyRequest.Headers.Authorization);
    }

    [TestMethod]
    public async Task ApplyAsync_WhenUserIsNull_ShouldNotAddAuthorizationHeader()
    {
        // Arrange
        var httpContext = new DefaultHttpContext { User = null! };
        var proxyRequest = new HttpRequestMessage();
        var context = new RequestTransformContext { HttpContext = httpContext, ProxyRequest = proxyRequest };

        // Act
        await _jwtTransform.ApplyAsync(context);

        // Assert
        Assert.IsNull(proxyRequest.Headers.Authorization);
    }

    [TestMethod]
    public async Task ApplyAsync_WhenIdentityIsNull_ShouldNotAddAuthorizationHeader()
    {
        // Arrange
        var principal = new ClaimsPrincipal();
        var httpContext = new DefaultHttpContext { User = principal };
        var proxyRequest = new HttpRequestMessage();
        var context = new RequestTransformContext { HttpContext = httpContext, ProxyRequest = proxyRequest };

        // Act
        await _jwtTransform.ApplyAsync(context);

        // Assert
        Assert.IsNull(proxyRequest.Headers.Authorization);
    }

    [TestMethod]
    public async Task ApplyAsync_WithMultipleClaims_ShouldGenerateTokenWithAllClaims()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user123"),
            new Claim(ClaimTypes.Email, "user@example.com"),
            new Claim(ClaimTypes.Role, "Admin"),
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var principal = new ClaimsPrincipal(identity);
        var expectedToken = _jwtTokenService.GenerateToken(principal);

        var httpContext = new DefaultHttpContext { User = principal };
        var proxyRequest = new HttpRequestMessage();
        var context = new RequestTransformContext { HttpContext = httpContext, ProxyRequest = proxyRequest };

        // Act
        await _jwtTransform.ApplyAsync(context);

        // Assert
        Assert.IsNotNull(proxyRequest.Headers.Authorization);
        Assert.AreEqual(expectedToken, proxyRequest.Headers.Authorization.Parameter);
    }

    [TestMethod]
    public async Task ApplyAsync_ShouldReturnCompletedValueTask()
    {
        // Arrange
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "user123") };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        var proxyRequest = new HttpRequestMessage();
        var context = new RequestTransformContext { HttpContext = httpContext, ProxyRequest = proxyRequest };

        // Act
        var result = _jwtTransform.ApplyAsync(context);

        // Assert
        Assert.IsTrue(result.IsCompletedSuccessfully);
    }
}
