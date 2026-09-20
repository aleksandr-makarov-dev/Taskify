using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Taskify.WebApi.Domain.Users;
using Taskify.WebApi.Infrastructure.Options;

namespace Taskify.WebApi.Services;

public sealed class JsonWebTokenService(IOptions<JsonWebTokenOptions> options, TimeProvider timeProvider)
    : IJsonWebTokenService
{
    private readonly JsonWebTokenOptions _jwtOptions = options.Value;

    public string CreateToken(User user, IList<string> roles)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(user.Email);

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
        };
        
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var expiresAtUtc = timeProvider.GetUtcNow().UtcDateTime.Add(_jwtOptions.Expiration);

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAtUtc,
            SigningCredentials = credentials,
            Issuer = _jwtOptions.Issuer,
            Audience = _jwtOptions.Audience,
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}