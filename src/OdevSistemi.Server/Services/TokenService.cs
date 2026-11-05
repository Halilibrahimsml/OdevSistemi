using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using OdevSistemi.Server.Entities;

namespace OdevSistemi.Server.Services;

/// <summary>appsettings.json içindeki "Jwt" bölümü.</summary>
public class JwtSettings
{
    public string Key { get; set; } = "";
    public string Issuer { get; set; } = "OdevSistemi";
    public string Audience { get; set; } = "OdevSistemi";
    public int ExpireHours { get; set; } = 8;
}

/// <summary>
/// Giriş yapan kullanıcıya JWT (JSON Web Token) üretir.
/// Token'ın içinde kullanıcının Id'si, adı ve rolü vardır; sunucunun gizli anahtarıyla imzalanır,
/// bu yüzden kimse içini değiştiremez.
/// </summary>
public class TokenService
{
    private readonly JwtSettings _settings;

    public TokenService(JwtSettings settings) => _settings = settings;

    public (string Token, DateTime ExpiresAt) CreateToken(AppUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role.ToString()),   // "Instructor" veya "Student"
            new("full_name", user.FullName),
            new("class_name", user.ClassName)
        };

        DateTime expiresAt = DateTime.UtcNow.AddHours(_settings.ExpireHours);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        };

        string token = new JsonWebTokenHandler().CreateToken(descriptor);
        return (token, expiresAt);
    }
}