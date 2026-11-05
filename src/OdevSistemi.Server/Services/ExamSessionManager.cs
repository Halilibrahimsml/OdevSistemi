using System.Collections.Concurrent;
using OdevSistemi.Shared.Contracts;

namespace OdevSistemi.Server.Services;

/// <summary>
/// Bellekte tutulan durum: kim bağlı, hangi sınav aktif, bitiş saati ne.
/// Singleton: tüm bağlantılar aynı nesneyi görür. 40 öğrenci aynı anda
/// bağlanıp kopabileceği için ConcurrentDictionary + lock kullanılır.
/// </summary>
public class ExamSessionManager
{
    /// <summary>Ağ gecikmesi toleransı (sn). Paket yoldayken süre dolarsa haksızlık olmasın diye.</summary>
    public const int NetworkToleranceSeconds = 2;

    private readonly ConcurrentDictionary<string, ConnectedStudentDto> _students = new();
    private readonly object _lock = new();

    public int? ActiveExamId { get; private set; }
    public DateTime? EndUtc { get; private set; }
    public StudentExamDto? ActivePayload { get; private set; } // geç bağlanan öğrenciye tekrar göndermek için

    // ---- Bağlantı yönetimi ----
    public void AddStudent(ConnectedStudentDto s) => _students[s.ConnectionId] = s;

    public ConnectedStudentDto? GetStudent(string connectionId)
        => _students.TryGetValue(connectionId, out var s) ? s : null;

    public ConnectedStudentDto? RemoveStudent(string connectionId)
        => _students.TryRemove(connectionId, out var s) ? s : null;

    public IReadOnlyList<ConnectedStudentDto> GetStudents()
        => _students.Values.OrderBy(s => s.StudentName).ToList();

    // ---- Sınav durumu ----
    public void StartExam(StudentExamDto payload)
    {
        lock (_lock)
        {
            ActiveExamId = payload.ExamId;
            EndUtc = payload.EndUtc;
            ActivePayload = payload;
        }
    }

    public void StopExam()
    {
        lock (_lock) { ActiveExamId = null; EndUtc = null; ActivePayload = null; }
    }

    /// <summary>Süre bittiyse gönderim kabul edilmez (Hedef 10).</summary>
    public bool IsSubmissionAllowed(int examId, DateTime nowUtc, out string reason)
    {
        lock (_lock)
        {
            if (ActiveExamId != examId) { reason = "Bu sınav aktif değil."; return false; }
            if (EndUtc is null || nowUtc > EndUtc.Value.AddSeconds(NetworkToleranceSeconds))
            { reason = "Sınav süresi doldu, gönderim kabul edilmedi."; return false; }
            reason = "";
            return true;
        }
    }
}
