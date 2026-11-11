using OdevSistemi.Shared;

namespace OdevSistemi.Server.Entities;

// Her sınıf veritabanında bir tabloya karşılık gelir.
// "Id" adlı alan otomatik olarak birincil anahtar (primary key) olur.

/// <summary>Users tablosu: eğitmen ve öğrenci hesapları.</summary>
public class AppUser
{
    public int Id { get; set; }
    public string Username { get; set; } = "";   //   
    public string PasswordHash { get; set; } = "";  //şifreyi değil şifrenin hash ini tutuyoruz.
    public string FullName { get; set; } = "";
    public UserRole Role { get; set; } //Eğitmen = 0, Öğrenci = 1. SQLite Viewer’da bu yüzden 0 ve 1
    public string ClassName { get; set; } = "";     // ör. "1-A" (eğitmen için boş)
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; //Kayıt oluşturulduğu anın zamanı otomatik
}

/// <summary>Questions tablosu: soru bankası.</summary>
public class Question
{
    public int Id { get; set; }
    public QuestionType Type { get; set; } //çoktan seçmeli=0, klasik=1
    public string Category { get; set; } = "";      // diziler,döngüler,fonksiyonlar yani soru konusu
    public string Text { get; set; } = "";
    public List<string> Options { get; set; } = new();  // sadece çoktan seçmelide dolu (A, B, C...)
    public string CorrectAnswer { get; set; } = "";      // çoktan seçmeli:Şıkkın numaraları "0","1"..  klasik: "printf|printf()"
    public double DefaultPoints { get; set; } = 10; //soru sınava eklenirken önerilen puan
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<ExamQuestion> ExamQuestions { get; set; } = new();
    //Bu soru hangi sınavlarda kullanılıyor? listesi. navigasyon özelliği
}

/// <summary>Exams tablosu: eğitmenin hazırladığı sınav / ödev.</summary>
public class Exam
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public int DurationMinutes { get; set; }//Eğitmenin belirlediği süre
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // sınavın oluşturulduğu zaman

    // Sınav başlatılınca doldurulur. Süre kontrolü bunlara göre yapılır.
    public DateTime? StartedAt { get; set; } //Boş(null) olabilir çünkü sınav başlatılmadan önce null olur
    public DateTime? EndsAt { get; set; } //Boş olabilir çünkü sınav bitmeden önce null olur

    public List<ExamQuestion> Questions { get; set; } = new(); 
    public List<Submission> Submissions { get; set; } = new();
}

/// <summary>ExamQuestions tablosu: hangi sınavda hangi soru var, sırası ve puanı ne.</summary>
public class ExamQuestion
{
    public int ExamId { get; set; }
    public Exam? Exam { get; set; }

    public int QuestionId { get; set; }//QuestionId veritabanında tutulan yabancı anahtar,sorunun numarası (sütun)
    public Question? Question { get; set; }//o numaraya karşılık gelen soru nesnesi

    public int OrderNo { get; set; }     // sınavdaki soru sırası: 1, 2, 3...
    public double Points { get; set; }   // bu sınavdaki puan ağırlığı
}

/// <summary>Submissions tablosu: öğrencinin sınav gönderimi ve toplam puanı.</summary>
public class Submission
{
    public int Id { get; set; }

//Hangi sınava gönderildiği (numara + navigasyon)
    public int ExamId { get; set; }
    public Exam? Exam { get; set; }

//gönderimi yapan öğrenci
    public int StudentId { get; set; }
    public AppUser? Student { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    //Gönderimin geldiği bilgisayarın IP adresi 
    public string ClientIp { get; set; } = "";

    public double Score { get; set; }
    public double MaxScore { get; set; }
    public int CorrectCount { get; set; }

    public List<SubmissionAnswer> Answers { get; set; } = new();
}

/// <summary>SubmissionAnswers tablosu: gönderimdeki her sorunun cevabı .</summary>
public class SubmissionAnswer
{
    public int Id { get; set; }

    public int SubmissionId { get; set; }
    public Submission? Submission { get; set; }

    public int QuestionId { get; set; }
    
    //öğrencinin cevabı
    public string Answer { get; set; } = "";
    public bool IsCorrect { get; set; }//doğru mu?
    public double Points { get; set; }
}

/// <summary>SystemLogs tablosu: giriş, sınav başlatma, gönderim gibi olayların kaydı.</summary>
public class SystemLog
{
    public int Id { get; set; }
    public DateTime Time { get; set; } = DateTime.UtcNow; // olayın gerçekleştiği zaman
    public string Category { get; set; } = "";   // ör. "Login", "Submission", "Exam"
    public string Message { get; set; } = "";
    public string? Username { get; set; }
    public string? Ip { get; set; }
}