namespace OdevSistemi.Shared.Contracts;

/// <summary>
/// SignalR metot/olay adları. İstemciler string yazmak yerine bunları kullanır,
/// böylece yazım hatası derleme anında yakalanır.
/// Örnek (öğrenci): conn.On&lt;StudentExamDto&gt;(HubEvents.ReceiveExam, ...)
/// </summary>
public static class HubEvents      // Sunucu -> İstemci
{
    public const string ReceiveExam = "ReceiveExam";
    public const string ExamEnded = "ExamEnded";
    public const string StudentListChanged = "StudentListChanged";
    public const string SubmissionReceived = "SubmissionReceived";
}

public static class HubMethods     // İstemci -> Sunucu
{
    public const string RegisterStudent = "RegisterStudent";
    public const string RegisterInstructor = "RegisterInstructor";
    public const string SubmitExam = "SubmitExam";
}

public static class ApiHeaders
{
    /// <summary>Geçici eğitmen koruması (Kişi 1'in JWT'si gelene kadar).</summary>
    public const string InstructorKey = "X-Instructor-Key";
}
