using Microsoft.EntityFrameworkCore;
using OdevSistemi.Server.Entities;

namespace OdevSistemi.Server.Data;

/// <summary>
/// Veritabanının C#'taki karşılığı. Her DbSet bir tablodur.
/// Entity Framework Core, SQL yazmadan bu tablolara veri ekleyip okumamızı sağlar.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<Exam> Exams => Set<Exam>();
    public DbSet<ExamQuestion> ExamQuestions => Set<ExamQuestion>();
    public DbSet<Submission> Submissions => Set<Submission>();
    public DbSet<SubmissionAnswer> SubmissionAnswers => Set<SubmissionAnswer>();
    public DbSet<SystemLog> SystemLogs => Set<SystemLog>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // Aynı kullanıcı adı iki kez kaydedilemez (büyük/küçük harf fark etmez).
        b.Entity<AppUser>().Property(u => u.Username).UseCollation("NOCASE");
        b.Entity<AppUser>().HasIndex(u => u.Username).IsUnique();

        // ExamQuestion'ın anahtarı iki alandan oluşur: (ExamId, QuestionId).
        b.Entity<ExamQuestion>().HasKey(eq => new { eq.ExamId, eq.QuestionId });

        // Sınav silinirse ona bağlı soru eşleştirmeleri de silinir.
        b.Entity<ExamQuestion>()
            .HasOne(eq => eq.Exam).WithMany(e => e.Questions)
            .HasForeignKey(eq => eq.ExamId).OnDelete(DeleteBehavior.Cascade);

        // Bir sınavda kullanılan soru silinemez (önce sınavdan çıkarılmalı).
        b.Entity<ExamQuestion>()
            .HasOne(eq => eq.Question).WithMany(q => q.ExamQuestions)
            .HasForeignKey(eq => eq.QuestionId).OnDelete(DeleteBehavior.Restrict);

        // Bir öğrenci aynı sınava sadece bir kez gönderim yapabilir.
        b.Entity<Submission>().HasIndex(s => new { s.ExamId, s.StudentId }).IsUnique();

        b.Entity<Submission>()
            .HasOne(s => s.Exam).WithMany(e => e.Submissions)
            .HasForeignKey(s => s.ExamId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<Submission>()
            .HasOne(s => s.Student).WithMany()
            .HasForeignKey(s => s.StudentId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<SubmissionAnswer>()
            .HasOne(a => a.Submission).WithMany(s => s.Answers)
            .HasForeignKey(a => a.SubmissionId).OnDelete(DeleteBehavior.Cascade);
    }
}