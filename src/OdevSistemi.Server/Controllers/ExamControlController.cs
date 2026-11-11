using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OdevSistemi.Server.Data;
using OdevSistemi.Server.Services;
using OdevSistemi.Shared.Contracts;

namespace OdevSistemi.Server.Controllers;

/// <summary>Eğitmen arayüzünün çağıracağı uç noktalar (sınav başlat/bitir, bağlı öğrenciler, sonuçlar).</summary>
[ApiController]
[Route("api/exam")]
[Authorize(Roles = "Instructor")]
public class ExamControlController : ControllerBase
{
    private readonly ExamBroadcaster _broadcaster;
    private readonly ExamSessionManager _session;
    private readonly AppDbContext _db;

    public ExamControlController(ExamBroadcaster b, ExamSessionManager s, AppDbContext db)
    {
        _broadcaster = b; _session = s; _db = db;
    }

    // POST api/exam/5/start  -> Vize İsteri 4 (eş zamanlı dağıtım)
    [HttpPost("{examId:int}/start")]
    public async Task<IActionResult> Start(int examId)
    {
        try
        {
            var payload = await _broadcaster.BroadcastExam(examId);
            return Ok(new { payload.ExamId, payload.EndUtc, sentTo = _session.GetStudents().Count });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // POST api/exam/stop
    [HttpPost("stop")]
    public async Task<IActionResult> Stop()
    {
        await _broadcaster.StopExam();
        return Ok();
    }

    // GET api/exam/connected
    [HttpGet("connected")]
    public ActionResult<IReadOnlyList<ConnectedStudentDto>> Connected() => Ok(_session.GetStudents());

    // GET api/exam/5/results  (öğrenci bazında puanlar)
    [HttpGet("{examId:int}/results")]
    public async Task<ActionResult<List<ExamResultRowDto>>> Results(int examId)
        => Ok(await _db.Submissions
            .Where(s => s.ExamId == examId)
            .OrderByDescending(s => s.Score)
            .Select(s => new ExamResultRowDto(
                s.StudentId,
                s.Student!.FullName,   // isim Users tablosundan
                s.Score,
                s.MaxScore,
                s.SubmittedAt,
                s.ClientIp))
            .ToListAsync());
}