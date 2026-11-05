using OdevSistemi.Shared;

namespace OdevSistemi.Server.Entities;

// Her sınıf veritabanında bir tabloya karşılık gelir.
// "Id" adlı alan otomatik olarak birincil anahtar (primary key) olur.

/// <summary>Users tablosu: eğitmen ve öğrenci hesapları.</summary>
public class AppUser
{
    public int Id { get; set; }
    public string Username { get; set; } = "";      // öğrenci numarası veya eğitmen kullanıcı adı
    public string PasswordHash { get; set; } = "";  // şifre asla düz metin saklanmaz
    public string FullName { get; set; } = "";
    public UserRole Role { get; set; }
    public string ClassName { get; set; } = "";     // ör. "1-A" (eğitmen için boş)
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Questions tablosu: soru bankası.</summary>
public class Question
{
    public int Id { get; set; }
    public QuestionType Type { get; set; }
    public string Category { get; set; } = "";      // ör. "Diziler", "Döngüler"
    public string Text { get; set; } = "";
    public List<string> Options { get; set; } = new();  // sadece çoktan seçmelide dolu (A, B, C...)
    public string CorrectAnswer { get; set; } = "";      // çoktan seçmeli: "0","1"..  klasik: "printf|printf()"
    public double DefaultPoints { get; set; } = 10;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<ExamQuestion> ExamQuestions { get; set; } = new();
}

/// <summary>Exams tablosu: eğitmenin hazırladığı sınav / ödev.</summary>
public class Exam
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public int DurationMinutes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Sınav başlatılınca doldurulur (2. kişi). Süre kontrolü bunlara göre yapılır.
    public DateTime? StartedAt { get; set; }
    public DateTime? EndsAt { get; set; }

    public List<ExamQuestion> Questions { get; set; } = new();
    public List<Submission> Submissions { get; set; } = new();
}

/// <summary>ExamQuestions tablosu: hangi sınavda hangi soru var, sırası ve puanı ne.</summary>
public class ExamQuestion
{
    public int ExamId { get; set; }
    public Exam? Exam { get; set; }

    public int QuestionId { get; set; }
    public Question? Question { get; set; }

    public int OrderNo { get; set; }     // sınavdaki sırası: 1, 2, 3...
    public double Points { get; set; }   // bu sınavdaki puan ağırlığı
}

/// <summary>Submissions tablosu: öğrencinin sınav gönderimi ve toplam puanı.</summary>
public class Submission
{
    public int Id { get; set; }

    public int ExamId { get; set; }
    public Exam? Exam { get; set; }

    public int StudentId { get; set; }
    public AppUser? Student { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public string ClientIp { get; set; } = "";
    public double Score { get; set; }
    public double MaxScore { get; set; }
    public int CorrectCount { get; set; }

    public List<SubmissionAnswer> Answers { get; set; } = new();
}

/// <summary>SubmissionAnswers tablosu: gönderimdeki her sorunun cevabı (2. kişinin puanlaması yazar).</summary>
public class SubmissionAnswer
{
    public int Id { get; set; }

    public int SubmissionId { get; set; }
    public Submission? Submission { get; set; }

    public int QuestionId { get; set; }
    public string Answer { get; set; } = "";
    public bool IsCorrect { get; set; }
    public double Points { get; set; }
}

/// <summary>SystemLogs tablosu: giriş, sınav başlatma, gönderim gibi olayların kaydı.</summary>
public class SystemLog
{
    public int Id { get; set; }
    public DateTime Time { get; set; } = DateTime.UtcNow;
    public string Category { get; set; } = "";   // ör. "Login", "Submission", "Exam"
    public string Message { get; set; } = "";
    public string? Username { get; set; }
    public string? Ip { get; set; }
}