using OdevSistemi.Shared;
namespace OdevSistemi.Shared.Contracts;

// Sunucu + öğrenci arayüzü + eğitmen arayüzü AYNI sınıfları kullanır.
// Bir alanı değiştirirseniz üç proje birden güncellenmiş olur.


// Öğrenciye giden soru: CorrectAnswer ve Keywords YOK (cevap sızmasın diye)
public record StudentQuestionDto(int Id, string Text, QuestionType Type, List<string>? Options, double Points, int Order);

// Öğrenciye giden sınav paketi
public record StudentExamDto(int ExamId, string Title, DateTime StartUtc, DateTime EndUtc, List<StudentQuestionDto> Questions);

// Öğrenciden gelen cevaplar (StudentId sunucuda bağlantıdan alınır, istemciye güvenilmez)
public record AnswerDto(int QuestionId, string? Answer);
public record SubmitExamDto(int ExamId, List<AnswerDto> Answers);

// Sunucunun SubmitExam'e verdiği cevap
public record SubmitResultDto(bool Success, string Message, double? Score = null, double? MaxScore = null);

// Puanlama sonucu
public record QuestionResult(int QuestionId, bool IsCorrect, double Earned, double Max);
public record GradingResult(double TotalScore, double MaxScore, List<QuestionResult> Details);

// Eğitmen ekranı için
public record ConnectedStudentDto(string ConnectionId, int StudentId, string StudentName, string? Ip, DateTime ConnectedAtUtc);
public record SubmissionNoticeDto(int StudentId, string StudentName, int ExamId, double TotalScore, double MaxScore, DateTime SubmittedAtUtc);
public record ExamResultRowDto(int StudentId, string StudentName, double TotalScore, double MaxScore, DateTime SubmittedAtUtc, string? ClientIp);
