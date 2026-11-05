using Microsoft.EntityFrameworkCore;
using OdevSistemi.Shared.Contracts;

namespace OdevSistemi.Server.Data;

// =====================================================================
// GEÇİCİ DOSYA — Kişi 1'in modelleri ve AppDbContext'i gelince SİLİNECEK.
// Sadece benim kodum derlenip test edilebilsin diye var. Kişi 1'den şu
// alanların kendi modellerinde bulunmasını isteyin (isimler farklı olabilir):
//   Question:   Id, Text, Type, Category, OptionsJson, CorrectAnswer, Keywords, Points
//   Exam:       Id, Title, DurationMinutes
//   ExamQuestion: ExamId, QuestionId, Order
//   Submission: Id, ExamId, StudentId, StudentName, SubmittedAtUtc, TotalScore, MaxScore, AnswersJson, ClientIp
// =====================================================================

public class Question
{
    public int Id { get; set; }
    public string Text { get; set; } = "";
    public QuestionType Type { get; set; }
    public string Category { get; set; } = "";
    public string? OptionsJson { get; set; }        // ÇS şıkları: ["A) ...","B) ..."]
    public string CorrectAnswer { get; set; } = ""; // ÇS: "B" | Klasik: örnek cevap
    public string? Keywords { get; set; }           // Klasik: "pointer,adres,bellek"
    public double Points { get; set; } = 10;
}

public class Exam
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public int DurationMinutes { get; set; }
    public List<ExamQuestion> ExamQuestions { get; set; } = new();
}

public class ExamQuestion
{
    public int ExamId { get; set; }
    public Exam Exam { get; set; } = null!;
    public int QuestionId { get; set; }
    public Question Question { get; set; } = null!;
    public int Order { get; set; }
}

public class Submission
{
    public int Id { get; set; }
    public int ExamId { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = "";
    public DateTime SubmittedAtUtc { get; set; }
    public double TotalScore { get; set; }
    public double MaxScore { get; set; }
    public string AnswersJson { get; set; } = "[]";
    public string? ClientIp { get; set; }
}

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> o) : base(o) { }

    public DbSet<Question> Questions => Set<Question>();
    public DbSet<Exam> Exams => Set<Exam>();
    public DbSet<ExamQuestion> ExamQuestions => Set<ExamQuestion>();
    public DbSet<Submission> Submissions => Set<Submission>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<ExamQuestion>().HasKey(eq => new { eq.ExamId, eq.QuestionId });
        // Aynı öğrenci aynı sınavı iki kez gönderemesin (DB seviyesinde garanti)
        b.Entity<Submission>().HasIndex(s => new { s.ExamId, s.StudentId }).IsUnique();
    }
}
