using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OdevSistemi.Server.Data;
using OdevSistemi.Server.Entities;
using OdevSistemi.Shared;
using OdevSistemi.Shared.Dtos;

namespace OdevSistemi.Server.Controllers;

/// <summary>
/// Sınav (Ödev) Hazırlama API'si (sadece eğitmen kullanabilir).
///   GET    /api/exams       → sınav listesi
///   GET    /api/exams/3     → sınav detayı (sorular, puanlar)
///   POST   /api/exams       → yeni sınav
///   PUT    /api/exams/3     → sınavı güncelle (başlatılmamışsa)
///   DELETE /api/exams/3     → sınavı sil (başlatılmamışsa)
/// Sınavı öğrencilere göndermek (başlatmak) 2. kişinin SignalR kısmındadır.
/// </summary>
[ApiController]
[Route("api/exams")]
[Authorize(Roles = nameof(UserRole.Instructor))]
public class ExamsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ExamsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<ExamSummaryDto>>> GetAll()
    {
        var exams = await _db.Exams
            .AsNoTracking()
            .OrderByDescending(e => e.Id)
            .Select(e => new ExamSummaryDto
            {
                Id = e.Id,
                Title = e.Title,
                DurationMinutes = e.DurationMinutes,
                QuestionCount = e.Questions.Count,
                TotalPoints = e.Questions.Sum(q => q.Points),
                SubmissionCount = e.Submissions.Count,
                CreatedAt = e.CreatedAt,
                StartedAt = e.StartedAt,
                EndsAt = e.EndsAt
            })
            .ToListAsync();

        foreach (var exam in exams)
            exam.Status = GetStatus(exam.StartedAt, exam.EndsAt);

        return exams;
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ExamDetailDto>> GetById(int id)
    {
        ExamDetailDto? detail = await LoadDetailAsync(id);
        if (detail is null)
            return NotFound(new { message = "Sınav bulunamadı." });

        return detail;
    }

    [HttpPost]
    public async Task<ActionResult<ExamDetailDto>> Create(ExamSaveRequest request)
    {
        var (error, items) = await BuildItemsAsync(request);
        if (error is not null)
            return BadRequest(new { message = error });

        var exam = new Exam
        {
            Title = request.Title.Trim(),
            DurationMinutes = request.DurationMinutes,
            Questions = items
        };

        _db.Exams.Add(exam);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = exam.Id }, await LoadDetailAsync(exam.Id));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ExamDetailDto>> Update(int id, ExamSaveRequest request)
    {
        Exam? exam = await _db.Exams.Include(e => e.Questions).FirstOrDefaultAsync(e => e.Id == id);
        if (exam is null)
            return NotFound(new { message = "Sınav bulunamadı." });

        if (exam.StartedAt is not null)
            return Conflict(new { message = "Başlatılmış bir sınav değiştirilemez." });

        var (error, items) = await BuildItemsAsync(request);
        if (error is not null)
            return BadRequest(new { message = error });

        exam.Title = request.Title.Trim();
        exam.DurationMinutes = request.DurationMinutes;

        // 1) Listeden çıkarılan soruları sınavdan sil.
        var wantedIds = items.Select(i => i.QuestionId).ToHashSet();
        foreach (var old in exam.Questions.Where(eq => !wantedIds.Contains(eq.QuestionId)).ToList())
            exam.Questions.Remove(old);

        // 2) Kalanların sırasını/puanını güncelle, yenileri ekle.
        foreach (var item in items)
        {
            ExamQuestion? existing = exam.Questions.FirstOrDefault(eq => eq.QuestionId == item.QuestionId);
            if (existing is null)
            {
                exam.Questions.Add(item);
            }
            else
            {
                existing.OrderNo = item.OrderNo;
                existing.Points = item.Points;
            }
        }

        await _db.SaveChangesAsync();
        return await LoadDetailAsync(id);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        Exam? exam = await _db.Exams.FindAsync(id);
        if (exam is null)
            return NotFound(new { message = "Sınav bulunamadı." });

        bool hasSubmissions = await _db.Submissions.AnyAsync(s => s.ExamId == id);
        if (exam.StartedAt is not null || hasSubmissions)
            return Conflict(new { message = "Uygulanmış bir sınav silinemez (sonuçlar kaybolur)." });

        _db.Exams.Remove(exam);   // ExamQuestions kayıtları da otomatik silinir (cascade)
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ---------------- yardımcı metodlar ----------------

    /// <summary>
    /// İsteği kontrol eder ve sınav-soru kayıtlarını hazırlar.
    /// Puan verilmemiş sorular için sorunun varsayılan puanı kullanılır.
    /// </summary>
    private async Task<(string? Error, List<ExamQuestion> Items)> BuildItemsAsync(ExamSaveRequest r)
    {
        var empty = new List<ExamQuestion>();

        if (string.IsNullOrWhiteSpace(r.Title))
            return ("Sınav başlığı boş olamaz.", empty);
        if (r.DurationMinutes < 1 || r.DurationMinutes > 600)
            return ("Süre 1 ile 600 dakika arasında olmalı.", empty);

        List<ExamQuestionItemRequest> list = r.Questions ?? new();
        if (list.Count == 0)
            return ("Sınava en az bir soru eklenmeli.", empty);

        List<int> ids = list.Select(q => q.QuestionId).ToList();
        if (ids.Distinct().Count() != ids.Count)
            return ("Aynı soru sınava iki kez eklenemez.", empty);

        var questions = await _db.Questions.Where(q => ids.Contains(q.Id)).ToDictionaryAsync(q => q.Id);
        List<int> missing = ids.Where(id => !questions.ContainsKey(id)).ToList();
        if (missing.Count > 0)
            return ($"Soru bulunamadı (Id: {string.Join(", ", missing)}).", empty);

        var items = new List<ExamQuestion>();
        for (int i = 0; i < list.Count; i++)
        {
            ExamQuestionItemRequest req = list[i];
            if (req.Points is not null && req.Points <= 0)
                return ($"{i + 1}. sorunun puanı 0'dan büyük olmalı.", empty);

            items.Add(new ExamQuestion
            {
                QuestionId = req.QuestionId,
                OrderNo = i + 1,
                Points = req.Points ?? questions[req.QuestionId].DefaultPoints
            });
        }

        return (null, items);
    }

    private async Task<ExamDetailDto?> LoadDetailAsync(int id)
    {
        Exam? exam = await _db.Exams
            .AsNoTracking()
            .Include(e => e.Questions).ThenInclude(eq => eq.Question)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (exam is null)
            return null;

        return new ExamDetailDto
        {
            Id = exam.Id,
            Title = exam.Title,
            DurationMinutes = exam.DurationMinutes,
            TotalPoints = exam.Questions.Sum(q => q.Points),
            CreatedAt = exam.CreatedAt,
            StartedAt = exam.StartedAt,
            EndsAt = exam.EndsAt,
            Status = GetStatus(exam.StartedAt, exam.EndsAt),
            Questions = exam.Questions
                .OrderBy(eq => eq.OrderNo)
                .Select(eq => new ExamQuestionDetailDto
                {
                    OrderNo = eq.OrderNo,
                    Points = eq.Points,
                    Question = new QuestionDto
                    {
                        Id = eq.Question!.Id,
                        Type = eq.Question.Type,
                        Category = eq.Question.Category,
                        Text = eq.Question.Text,
                        Options = eq.Question.Options.ToList(),
                        CorrectAnswer = eq.Question.CorrectAnswer,
                        DefaultPoints = eq.Question.DefaultPoints
                    }
                })
                .ToList()
        };
    }

    private static string GetStatus(DateTime? startedAt, DateTime? endsAt)
    {
        if (startedAt is null) return "Hazır";
        if (endsAt is not null && DateTime.UtcNow > endsAt) return "Bitti";
        return "Devam ediyor";
    }
}