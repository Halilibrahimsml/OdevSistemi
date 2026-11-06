namespace OdevSistemi.StudentUI;

/*
 Beklenen sınav paketi (ReceiveExam), doğru cevap alanları sunucuda temizlenmiş gelir:
 {
   "examId": 1, "title": "Vize Sınavı", "durationMinutes": 30, "remainingSeconds": 1800,
   "questions": [
     { "id": 10, "type": "MultipleChoice", "text": "...", "points": 5,
       "options": [ { "key": "A", "text": "..." }, { "key": "B", "text": "..." } ] },
     { "id": 11, "type": "Classic", "text": "...", "points": 10 }
   ]
 }
*/

public class LoginRequest
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}

public class LoginResponse
{
    public string Token { get; set; } = "";
    public int UserId { get; set; }
    public string FullName { get; set; } = "";
    public string Role { get; set; } = "";
}

public class ExamOption
{
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
}

public class AnswerItem
{
    public int QuestionId { get; set; }
    public string Answer { get; set; } = "";
}

public class SubmissionRequest
{
    public int ExamId { get; set; }
    public int StudentId { get; set; }
    public List<AnswerItem> Answers { get; set; } = new();
}
