using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RetailSystem.Api.Models;

namespace RetailSystem.Api.Services;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "RetailSystem.Api";
    public string Audience { get; set; } = "RetailSystem.Web";
    public string SecretKey { get; set; } = "RetailSystem_Development_Secret_Key_Change_Me_2026";
    public int ExpirationMinutes { get; set; } = 120;
}

public sealed record GeneratedToken(string Value, DateTime ExpiresAt);

public sealed class JwtService(IOptions<JwtOptions> options)
{
    private readonly JwtOptions _options = options.Value;

    public GeneratedToken Generate(User user)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_options.ExpirationMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt,
            signingCredentials: credentials);

        return new GeneratedToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
