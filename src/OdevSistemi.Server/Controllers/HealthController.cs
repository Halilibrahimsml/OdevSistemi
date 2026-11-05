using Microsoft.AspNetCore.Mvc;

namespace OdevSistemi.Server.Controllers;

/// <summary>Sunucu çalışıyor mu? Tarayıcıda http://localhost:5000/api/health</summary>
[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "ok", time = DateTime.Now });
}
