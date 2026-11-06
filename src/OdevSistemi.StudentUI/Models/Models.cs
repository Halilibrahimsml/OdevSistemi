using OdevSistemi.Shared.Contracts;

namespace OdevSistemi.StudentUI;

// Arayüzün kendi iç modelleri.
// Sunucudan gelen StudentExamDto (Shared) burada ExamPackage'a çevrilir; ekranlar ve yerel yedek bunu kullanır.
// Giriş ve gönderim için Shared'daki LoginRequest / LoginResponse / SubmitExamDto kullanılır.

public class ExamOption
{
    /// <summary>"A", "B", "C"... Sunucuya da bu harf gönderilir (puanlama harfi kabul ediyor: "B" = "1").</summary>
    public string Key { get; set; } = "";
    public string Text { get; set; } = "";
}

public class ExamQuestion
{
    public int Id { get; set; }
    public string Type { get; set; } = "MultipleChoice";   // "MultipleChoice" | "Classic"
    public string Text { get; set; } = "";
    public double Points { get; set; }
    public List<ExamOption> Options { get; set; } = new();
}

public class ExamPackage
{
    public int ExamId { get; set; }
    public string Title { get; set; } = "Sınav";
    public int DurationMinutes { get; set; }
    public int? RemainingSeconds { get; set; }
    public List<ExamQuestion> Questions { get; set; } = new();

    /// <summary>Sunucunun gönderdiği sınav paketini ekranın kullandığı modele çevirir.</summary>
    public static ExamPackage FromDto(StudentExamDto dto)
    {
        // Toplam süre ve kalan süre sunucunun saatine (StartUtc/EndUtc) göre hesaplanır.
        // Kalan süreden güvenlik payı düşülür: sayaç biraz erken biter, gönderim süre içinde yetişir.
        int total = Math.Max(1, (int)Math.Round((dto.EndUtc - dto.StartUtc).TotalSeconds));
        int left = (int)Math.Floor((dto.EndUtc - DateTime.UtcNow).TotalSeconds) - AppConfig.AutoSubmitSafetySeconds;

        return new ExamPackage
        {
            ExamId = dto.ExamId,
            Title = dto.Title,
            DurationMinutes = (int)Math.Ceiling(total / 60.0),
            RemainingSeconds = Math.Clamp(left, 0, total),
            Questions = dto.Questions
                .OrderBy(q => q.Order)
                .Select(q => new ExamQuestion
                {
                    Id = q.Id,
                    Type = q.Type.ToString(),   // QuestionType.MultipleChoice -> "MultipleChoice"
                    Text = q.Text,
                    Points = q.Points,
                    Options = (q.Options ?? new List<string>())
                        .Select((text, i) => new ExamOption { Key = ((char)('A' + i)).ToString(), Text = text })
                        .ToList()
                })
                .ToList()
        };
    }
}
