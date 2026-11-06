using OdevSistemi.Server.Entities;
using OdevSistemi.Server.Services;
using OdevSistemi.Shared;

namespace OdevSistemi.Server.Data;

/// <summary>Veritabanı boşsa başlangıç hesaplarını ve örnek soruları ekler.</summary>
public static class DbSeeder
{
    public static void Seed(AppDbContext db)
    {
        SeedUsers(db);
        SeedQuestions(db);
    }

    private static void SeedUsers(AppDbContext db)
    {
        if (db.Users.Any())
            return;

        db.Users.AddRange(
            new AppUser { Username = "admin", PasswordHash = PasswordHasher.Hash("admin123"), FullName = "Eğitmen (Yönetici)", Role = UserRole.Instructor },
            new AppUser { Username = "230229035", PasswordHash = PasswordHasher.Hash("1234"), FullName = "Halil İbrahim Şamlı", Role = UserRole.Student, ClassName = "Yazılım-3.sınıf" },
            new AppUser { Username = "230229060", PasswordHash = PasswordHasher.Hash("1234"), FullName = "Arda Kundak", Role = UserRole.Student, ClassName = "Yazılım-3.sınıf" },
            new AppUser { Username = "260229090", PasswordHash = PasswordHasher.Hash("1234"), FullName = "Şevval Bayrak", Role = UserRole.Student, ClassName = "Yazılım-4.sınıf" },
            new AppUser { Username = "240229065", PasswordHash = PasswordHasher.Hash("1234"), FullName = "Berivan Yıldırım", Role = UserRole.Student, ClassName = "Yazılım-3.sınıf" });

        db.SaveChanges();
    }

    private static void SeedQuestions(AppDbContext db)
    {
        if (db.Questions.Any())
            return;

        db.Questions.AddRange(
            new Question
            {
                Type = QuestionType.MultipleChoice,
                Category = "C Temelleri",
                Text = "C dilinde bir değişkenin bellekte kapladığı bayt sayısını öğrenmek için hangi operatör kullanılır?",
                Options = ["sizeof", "typeof", "lengthof", "memsize"],
                CorrectAnswer = "0"
            },
            new Question
            {
                Type = QuestionType.MultipleChoice,
                Category = "Karmaşıklık",
                Text = "Sıralı bir dizide ikili arama (binary search) algoritmasının en kötü durum zaman karmaşıklığı nedir?",
                Options = ["O(1)", "O(log n)", "O(n)", "O(n log n)"],
                CorrectAnswer = "1"
            },
            new Question
            {
                Type = QuestionType.MultipleChoice,
                Category = "Diziler",
                Text = "int dizi[10]; tanımında dizinin son elemanının indisi kaçtır?",
                Options = ["10", "9", "11", "0"],
                CorrectAnswer = "1"
            },
            new Question
            {
                Type = QuestionType.Classic,
                Category = "C Temelleri",
                Text = "C dilinde standart çıktıya biçimlendirilmiş yazı yazdıran fonksiyonun adı nedir?",
                CorrectAnswer = "printf|printf()"
            });

        db.SaveChanges();
    }
}