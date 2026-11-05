using System.Globalization;
using Microsoft.EntityFrameworkCore;
using OdevSistemi.Server.Data;
using OdevSistemi.Server.Entities;
using OdevSistemi.Shared.Contracts;

namespace OdevSistemi.Server.Services;

/// <summary>Vize İsteri 7: cevap anahtarına göre anında puanlama + veritabanına yazma.</summary>
public class GradingService
{
    private readonly AppDbContext _db;
    private static readonly CultureInfo Tr = new("tr-TR");

    public GradingService(AppDbContext db) => _db = db;

    /// <param name="studentName">Artık veritabanına yazılmıyor (öğrenci bilgisi Users tablosunda). Uyumluluk için duruyor.</param>
    public async Task<(Submission sub, GradingResult result)> GradeAndSave(
        SubmitExamDto dto, int studentId, string studentName, string? ip)
    {
        // Aynı öğrenci aynı sınavı iki kez gönderemesin
        bool already = await _db.Submissions.AnyAsync(s => s.ExamId == dto.ExamId && s.StudentId == studentId);
        if (already) throw new InvalidOperationException("Bu sınavı zaten gönderdiniz.");

        // Sınavdaki sorular, sırasıyla ve bu sınavdaki puanlarıyla
        List<ExamQuestion> items = await _db.ExamQuestions
            .Where(eq => eq.ExamId == dto.ExamId)
            .Include(eq => eq.Question)
            .OrderBy(eq => eq.OrderNo)
            .AsNoTracking()
            .ToListAsync();

        List<AnswerDto> answers = dto.Answers ?? new();
        GradingResult result = Grade(items, answers);

        var answerMap = AnswerMap(answers);

        var sub = new Submission
        {
            ExamId = dto.ExamId,
            StudentId = studentId,
            SubmittedAt = DateTime.UtcNow,
            ClientIp = ip ?? "",
            Score = result.TotalScore,
            MaxScore = result.MaxScore,
            CorrectCount = result.Details.Count(d => d.IsCorrect),
            Answers = result.Details.Select(d => new SubmissionAnswer
            {
                QuestionId = d.QuestionId,
                Answer = answerMap.GetValueOrDefault(d.QuestionId, ""),
                IsCorrect = d.IsCorrect,
                Points = d.Earned
            }).ToList()
        };

        _db.Submissions.Add(sub);
        await _db.SaveChangesAsync();
        return (sub, result);
    }

    /// <summary>Saf puanlama fonksiyonu (DB'siz) — birim testi yazmak kolay olsun diye ayrı.</summary>
    public static GradingResult Grade(List<ExamQuestion> items, List<AnswerDto> answers)
    {
        var answerMap = AnswerMap(answers);
        var details = new List<QuestionResult>();
        double total = 0, max = 0;

        foreach (var item in items)
        {
            Question q = item.Question!;
            double points = item.Points;   // puan sınava özel: ExamQuestion.Points
            max += points;

            string given = answerMap.GetValueOrDefault(q.Id, "");

            double earned = q.Type switch
            {
                OdevSistemi.Shared.QuestionType.MultipleChoice => GradeMultipleChoice(q, points, given),
                OdevSistemi.Shared.QuestionType.Classic => GradeClassic(q, points, given),
                _ => 0
            };

            total += earned;
            details.Add(new QuestionResult(q.Id, earned >= points, earned, points));
        }

        return new GradingResult(Math.Round(total, 2), max, details);
    }

    // Çoktan seçmeli: CorrectAnswer şık numarasıdır ("0" = A, "1" = B ...).
    // Öğrenci harf ("B") ya da numara ("1") gönderebilir; ikisi de kabul edilir, büyük/küçük harf fark etmez.
    private static double GradeMultipleChoice(Question q, double points, string given)
    {
        string g = given.Trim();
        if (g == "" || !int.TryParse(q.CorrectAnswer, out int index))
            return 0;

        string letter = ((char)('A' + index)).ToString();
        bool correct = string.Equals(g, letter, StringComparison.OrdinalIgnoreCase) || g == index.ToString();
        return correct ? points : 0;
    }

    // Klasik: büyük/küçük harf duyarsız.
    // CorrectAnswer içinde kabul edilen cevaplar / anahtar kelimeler "|" ile ayrılır: "printf|printf()".
    // Öğrencinin cevabı bunlardan birine eşitse veya birini içeriyorsa tam puan.
    private static double GradeClassic(Question q, double points, string given)
    {
        string text = Normalize(given);
        if (text == "") return 0;

        List<string> keys = q.CorrectAnswer
            .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Normalize)
            .Where(k => k != "")
            .ToList();

        bool correct = keys.Any(k => text == k || text.Contains(k));
        return correct ? points : 0;
    }

    private static Dictionary<int, string> AnswerMap(List<AnswerDto> answers) =>
        answers
            .GroupBy(a => a.QuestionId)
            .ToDictionary(g => g.Key, g => g.Last().Answer ?? "");

    // Türkçe "İ/ı" sorununa karşı tr-TR kültürüyle küçült, fazla boşlukları teke indir.
    private static string Normalize(string s)
        => string.Join(' ', s.Trim().ToLower(Tr).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}