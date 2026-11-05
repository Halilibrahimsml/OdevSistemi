namespace OdevSistemi.Shared.Dtos;

/// <summary>Sınava eklenecek bir soru ve o sınavdaki puanı.</summary>
public class ExamQuestionItemRequest
{
    public int QuestionId { get; set; }
    /// <summary>Boş bırakılırsa sorunun varsayılan puanı kullanılır.</summary>
    public double? Points { get; set; }
}

/// <summary>Sınav oluştururken / güncellerken eğitmen arayüzünün gönderdiği bilgi.</summary>
public class ExamSaveRequest
{
    public string Title { get; set; } = "";
    public int DurationMinutes { get; set; } = 30;
    /// <summary>Soruların listedeki sırası, sınavdaki sırasıdır.</summary>
    public List<ExamQuestionItemRequest> Questions { get; set; } = new();
}

/// <summary>Sınav listesindeki her satır.</summary>
public class ExamSummaryDto
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public int DurationMinutes { get; set; }
    public int QuestionCount { get; set; }
    public double TotalPoints { get; set; }
    public int SubmissionCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndsAt { get; set; }

    /// <summary>"Hazır", "Devam ediyor" veya "Bitti".</summary>
    public string Status { get; set; } = "";
}

/// <summary>Sınavdaki bir soru (eğitmen için, cevap anahtarı dahil).</summary>
public class ExamQuestionDetailDto
{
    public int OrderNo { get; set; }
    public double Points { get; set; }
    public QuestionDto Question { get; set; } = new();
}

/// <summary>Tek bir sınavın tüm detayı.</summary>
public class ExamDetailDto
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public int DurationMinutes { get; set; }
    public double TotalPoints { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public string Status { get; set; } = "";
    public List<ExamQuestionDetailDto> Questions { get; set; } = new();
}