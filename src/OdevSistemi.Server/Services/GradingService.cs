using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OdevSistemi.Server.Data;
using OdevSistemi.Shared.Contracts;

namespace OdevSistemi.Server.Services;

/// <summary>Vize İsteri 7: cevap anahtarına göre anında puanlama + veritabanına yazma.</summary>
public class GradingService
{
    private readonly AppDbContext _db;
    private static readonly CultureInfo Tr = new("tr-TR");

    public GradingService(AppDbContext db) => _db = db;

    public async Task<(Submission sub, GradingResult result)> GradeAndSave(
        SubmitExamDto dto, int studentId, string studentName, string? ip)
    {
        // Aynı öğrenci aynı sınavı iki kez gönderemesin
        bool already = await _db.Submissions.AnyAsync(s => s.ExamId == dto.ExamId && s.StudentId == studentId);
        if (already) throw new InvalidOperationException("Bu sınavı zaten gönderdiniz.");

        var questions = await _db.ExamQuestions
            .Where(eq => eq.ExamId == dto.ExamId)
            .Select(eq => eq.Question)
            .AsNoTracking()
            .ToListAsync();

        var result = Grade(questions, dto.Answers);

        var sub = new Submission
        {
            ExamId = dto.ExamId,
            StudentId = studentId,
            StudentName = studentName,
            SubmittedAtUtc = DateTime.UtcNow,
            TotalScore = result.TotalScore,
            MaxScore = result.MaxScore,
            AnswersJson = JsonSerializer.Serialize(dto.Answers),
            ClientIp = ip
        };
        _db.Submissions.Add(sub);
        await _db.SaveChangesAsync();
        return (sub, result);
    }

    /// <summary>Saf puanlama fonksiyonu (DB'siz) — birim testi yazmak kolay olsun diye ayrı.</summary>
    public static GradingResult Grade(List<Question> questions, List<AnswerDto> answers)
    {
        var answerMap = answers
            .GroupBy(a => a.QuestionId)
            .ToDictionary(g => g.Key, g => g.Last().Answer ?? "");

        var details = new List<QuestionResult>();
        double total = 0, max = 0;

        foreach (var q in questions)
        {
            max += q.Points;
            answerMap.TryGetValue(q.Id, out var given);
            given ??= "";

            double earned = q.Type switch
            {
                QuestionType.MultipleChoice => GradeMultipleChoice(q, given),
                QuestionType.Classic => GradeClassic(q, given),
                _ => 0
            };

            total += earned;
            details.Add(new QuestionResult(q.Id, earned >= q.Points, earned, q.Points));
        }

        return new GradingResult(Math.Round(total, 2), max, details);
    }

    // Çoktan seçmeli: tam eşleşme (boşluklar kırpılır, "b" ile "B" aynı sayılır)
    private static double GradeMultipleChoice(Question q, string given)
        => given.Trim() != "" && Normalize(given) == Normalize(q.CorrectAnswer) ? q.Points : 0;

    // Klasik: büyük/küçük harf duyarsız.
    //  - Anahtar kelime varsa: bulunan kelime oranı kadar kısmi puan.
    //  - Yoksa: örnek cevapla birebir (normalize) eşleşme.
    private static double GradeClassic(Question q, string given)
    {
        var text = Normalize(given);
        if (text == "") return 0;

        var keywords = (q.Keywords ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Normalize)
            .Where(k => k != "")
            .ToList();

        if (keywords.Count == 0)
            return text == Normalize(q.CorrectAnswer) ? q.Points : 0;

        int found = keywords.Count(k => text.Contains(k));
        return Math.Round(q.Points * found / keywords.Count, 2);
    }

    // Türkçe "İ/ı" sorununa karşı tr-TR kültürüyle küçült, fazla boşlukları teke indir.
    private static string Normalize(string s)
        => string.Join(' ', s.Trim().ToLower(Tr).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
