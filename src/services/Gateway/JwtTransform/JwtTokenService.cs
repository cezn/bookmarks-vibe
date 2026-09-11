using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Gateway.JwtTransform;

public class JwtTokenService(IOptions<JwtOptions> options)
{
    private readonly SigningCredentials _credentials = new(
        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Value.Key)),
        SecurityAlgorithms.HmacSha256
    );
    private readonly JsonWebTokenHandler _handler = new();

    public string GenerateToken(ClaimsPrincipal user) =>
        _handler.CreateToken(
            new SecurityTokenDescriptor
            {
                Issuer = options.Value.Issuer,
                Audience = options.Value.Audience,
                Expires = DateTime.UtcNow.Add(options.Value.Lifetime),
                Subject = new ClaimsIdentity(user.Claims), // todo: check if we need to pick a correct schema
                SigningCredentials = _credentials,
            }
        );
}
