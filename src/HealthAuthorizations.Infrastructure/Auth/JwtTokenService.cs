using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace HealthAuthorizations.Infrastructure.Auth;

public sealed record IssuedToken(string AccessToken, DateTimeOffset ExpiresAt);

/// <summary>
/// Emisor de tokens para entornos locales/demostración. En un entorno real
/// el token lo emitiría un proveedor de identidad (Entra ID, Keycloak, etc.).
/// </summary>
public sealed class JwtTokenService(IOptions<JwtOptions> jwtOptions, IOptions<List<DemoUserOptions>> users, TimeProvider timeProvider)
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    public IssuedToken? TryIssue(string userName, string password)
    {
        var user = users.Value.FirstOrDefault(u =>
            string.Equals(u.UserName, userName, StringComparison.OrdinalIgnoreCase) &&
            CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(u.Password), Encoding.UTF8.GetBytes(password)));

        if (user is null)
        {
            return null;
        }

        var now = timeProvider.GetUtcNow();
        var expires = now.AddMinutes(_jwt.ExpirationMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Name, user.UserName),
        };
        claims.AddRange(user.Roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _jwt.Issuer,
            Audience = _jwt.Audience,
            Subject = new ClaimsIdentity(claims),
            NotBefore = now.UtcDateTime,
            Expires = expires.UtcDateTime,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SigningKey)),
                SecurityAlgorithms.HmacSha256),
        };

        return new IssuedToken(new JsonWebTokenHandler().CreateToken(descriptor), expires);
    }
}
