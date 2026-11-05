using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OdevSistemi.Server.Data;
using OdevSistemi.Server.Entities;
using OdevSistemi.Server.Services;
using OdevSistemi.Shared.Dtos;

namespace OdevSistemi.Server.Controllers;

/// <summary>
/// Giriş API'si.
///   POST /api/auth/login  → kullanıcı adı + şifre ile giriş, token döner
///   GET  /api/auth/me     → token sahibinin bilgileri (token gerekli)
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TokenService _tokens;

    public AuthController(AppDbContext db, TokenService tokens)
    {
        _db = db;
        _tokens = tokens;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrEmpty(request.Password))
            return BadRequest(new { message = "Kullanıcı adı ve şifre gerekli." });

        string username = request.Username.Trim();
        string ip = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "";

        AppUser? user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);

        if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            await AddLogAsync("Başarısız giriş denemesi", username, ip);
            return Unauthorized(new { message = "Kullanıcı adı veya şifre hatalı." });
        }

        // Rol kontrolü: öğrenci eğitmen uygulamasına, eğitmen öğrenci uygulamasına giremez.
        if (request.RequiredRole is not null && user.Role != request.RequiredRole)
        {
            await AddLogAsync($"Yanlış uygulamadan giriş denemesi ({user.Role})", username, ip);
            string message = user.Role == Shared.UserRole.Student
                ? "Bu uygulamaya sadece eğitmenler giriş yapabilir."
                : "Bu uygulamaya sadece öğrenciler giriş yapabilir.";
            return StatusCode(StatusCodes.Status403Forbidden, new { message });
        }

        var (token, expiresAt) = _tokens.CreateToken(user);
        await AddLogAsync("Giriş yaptı", username, ip);

        return new LoginResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = ToDto(user)
        };
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me()
    {
        // Token içindeki kullanıcı Id'sini oku.
        string? idText = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(idText, out int id))
            return Unauthorized();

        AppUser? user = await _db.Users.FindAsync(id);
        if (user is null)
            return NotFound();

        return ToDto(user);
    }

    private static UserDto ToDto(AppUser u) => new()
    {
        Id = u.Id,
        Username = u.Username,
        FullName = u.FullName,
        Role = u.Role,
        ClassName = u.ClassName
    };

    private async Task AddLogAsync(string message, string username, string ip)
    {
        _db.SystemLogs.Add(new SystemLog
        {
            Category = "Login",
            Message = message,
            Username = username,
            Ip = ip
        });
        await _db.SaveChangesAsync();
    }
}