using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OdevSistemi.Server.Data;
using OdevSistemi.Server.Entities;
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
        if (_session.ActivePayload is { } running && DateTime.UtcNow < running.EndUtc)
            throw new InvalidOperationException("Şu anda devam eden bir sınav var. Önce onu bitirin.");

        Exam exam = await _db.Exams
            .Include(e => e.Questions).ThenInclude(eq => eq.Question)
            .FirstOrDefaultAsync(e => e.Id == examId)
            ?? throw new InvalidOperationException("Sınav bulunamadı.");

        if (exam.StartedAt is not null)
            throw new InvalidOperationException("Bu sınav daha önce başlatılmış. Tekrar uygulamak için yeni bir sınav oluşturun.");
        if (exam.Questions.Count == 0)
            throw new InvalidOperationException("Sınavda hiç soru yok.");

        var start = DateTime.UtcNow;
        var end = start.AddMinutes(exam.DurationMinutes); // bitiş saati SUNUCUDA hesaplanır

        // Veritabanına da yaz: sınav listesinde "Devam ediyor" görünsün, başlamış sınav düzenlenemesin.
        exam.StartedAt = start;
        exam.EndsAt = end;
        await _db.SaveChangesAsync();

        // ÖNEMLİ: CorrectAnswer burada KOPYALANMIYOR -> öğrenciye hiç gitmiyor.
        var questions = exam.Questions
            .OrderBy(eq => eq.OrderNo)
            .Select(eq => new StudentQuestionDto(
                eq.Question!.Id,
                eq.Question.Text,
                eq.Question.Type,
                eq.Question.Options.Count > 0 ? eq.Question.Options.ToList() : null,
                eq.Points,              // puan sınava özel
                eq.OrderNo))
            .ToList();

        var payload = new StudentExamDto(exam.Id, exam.Title, start, end, questions);
        _session.StartExam(payload);

        // "Students" grubundaki herkese tek çağrıda gönder (eş zamanlı dağıtım)
        await _hub.Clients.Group(ExamHub.StudentsGroup).SendAsync(HubEvents.ReceiveExam, payload);
        return payload;
    }

    public async Task StopExam()
    {
        int? examId = _session.ActivePayload?.ExamId;
        _session.StopExam();        await _hub.Clients.Group(ExamHub.StudentsGroup).SendAsync(HubEvents.ExamEnded);
        if (examId is int id)
{
    var exam = await _db.Exams.FindAsync(id);
    if (exam is not null && (exam.EndsAt is null || exam.EndsAt > DateTime.UtcNow))
    {
        exam.EndsAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
}
    }
}