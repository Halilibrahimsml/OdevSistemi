namespace OdevSistemi.StudentUI;

/// <summary>
/// 2. Kişiyle birlikte netleştirilecek ortak ayarlar.
/// Bir şey değişirse sadece burayı güncellemen yeterli.
/// </summary>
public static class AppConfig
{
    public const int Port = 5000;
    public const string LoginPath = "/api/auth/login";        // POST {username, password} -> {token, userId, fullName, role}
    public const string HubPath = "/examHub";                  // SignalR hub adresi
    public const string SubmitPath = "/api/submissions";       // POST {examId, studentId, answers:[{questionId, answer}]}
    public const string EventReceiveExam = "ReceiveExam";      // sunucu -> öğrenci: sınav paketi
    public const string EventExamEnded = "ExamEnded";          // sunucu -> öğrenci: (opsiyonel) sınav bitti

    /// <summary>Gerekirse öğrenci kaydı için hub metodu adı, örn. "RegisterStudent".</summary>
    public static readonly string? RegisterMethod = null;
}
