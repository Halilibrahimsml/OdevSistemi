using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using OdevSistemi.Server.Services;

namespace OdevSistemi.Server.Controllers;

/// <summary>Vize İsteri 6 / Hedef 12: log dosyalarını eğitmen arayüzüne sunar.</summary>
[ApiController]
[Route("api/logs")]
[Authorize(Roles = "Instructor")]// Kişi 1 JWT ekleyince: [Authorize(Roles = "Instructor")]
public class LogsController : ControllerBase
{
    private readonly SubmissionLogger _logger;
    public LogsController(SubmissionLogger logger) => _logger = logger;

    // GET api/logs  -> ["submissions-2026-11-05.log", ...]
    [HttpGet]
    public IActionResult List() => Ok(_logger.ListLogFiles());

    // GET api/logs/submissions-2026-11-05.log?filter=SUBMIT
    [HttpGet("{fileName}")]
    public async Task<IActionResult> Read(string fileName, [FromQuery] string? filter)
    {
        var lines = await _logger.ReadLogAsync(fileName);
        if (!string.IsNullOrWhiteSpace(filter))
            lines = lines.Where(l => l.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToArray();
        return Ok(lines);
    }
}
