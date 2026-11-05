using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OdevSistemi.Server.Data;
using OdevSistemi.Server.Hubs;
using OdevSistemi.Shared.Contracts;

namespace OdevSistemi.Server.Services;

/// <summary>Vize İsteri 4 (dağıtım): sınavı tüm bağlı öğrencilere aynı anda gönderir.</summary>
public class ExamBroadcaster
{
    private readonly AppDbContext _db;
    private readonly IHubContext<ExamHub> _hub;
    private readonly ExamSessionManager _session;

    public ExamBroadcaster(AppDbContext db, IHubContext<ExamHub> hub, ExamSessionManager session)
    {
        _db = db; _hub = hub; _session = session;
    }

    public async Task<StudentExamDto> BroadcastExam(int examId)
    {
        var exam = await _db.Exams
            .Include(e => e.ExamQuestions).ThenInclude(eq => eq.Question)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == examId)
            ?? throw new InvalidOperationException("Sınav bulunamadı.");

        var start = DateTime.UtcNow;
        var end = start.AddMinutes(exam.DurationMinutes); // bitiş saati SUNUCUDA hesaplanır

        // ÖNEMLİ: CorrectAnswer ve Keywords burada KOPYALANMIYOR -> öğrenciye hiç gitmiyor.
        var questions = exam.ExamQuestions
            .OrderBy(eq => eq.Order)
            .Select(eq => new StudentQuestionDto(
                eq.Question.Id,
                eq.Question.Text,
                eq.Question.Type,
                eq.Question.OptionsJson is null ? null
                    : JsonSerializer.Deserialize<List<string>>(eq.Question.OptionsJson),
                eq.Question.Points,
                eq.Order))
            .ToList();

        var payload = new StudentExamDto(exam.Id, exam.Title, start, end, questions);
        _session.StartExam(payload);

        // "Students" grubundaki herkese tek çağrıda gönder (eş zamanlı dağıtım)
        await _hub.Clients.Group(ExamHub.StudentsGroup).SendAsync(HubEvents.ReceiveExam, payload);
        return payload;
    }

    public async Task StopExam()
    {
        _session.StopExam();
        await _hub.Clients.Group(ExamHub.StudentsGroup).SendAsync(HubEvents.ExamEnded);
    }
}
