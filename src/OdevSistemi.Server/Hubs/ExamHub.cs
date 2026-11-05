using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OdevSistemi.Server.Services;
using OdevSistemi.Shared.Contracts;

namespace OdevSistemi.Server.Hubs;

/// <summary>
/// Vize İsteri 2 + 6: öğrenci bağlantıları, kopma yönetimi ve cevap gönderimi.
/// İstemciler şu adrese bağlanır: http://EGITMEN_IP:{ServerInfo.Port}{ServerInfo.HubPath}?access_token=TOKEN
/// Token olmadan bağlanılamaz ([Authorize]).
/// </summary>
[Authorize]
public class ExamHub : Hub
{
    public const string StudentsGroup = "Students";
    public const string InstructorsGroup = "Instructors";

    private readonly ExamSessionManager _session;
    private readonly GradingService _grading;
    private readonly SubmissionLogger _logger;

    public ExamHub(ExamSessionManager session, GradingService grading, SubmissionLogger logger)
    {
        _session = session; _grading = grading; _logger = logger;
    }

    private string? ClientIp => Context.GetHttpContext()?.Connection.RemoteIpAddress?.MapToIPv4().ToString();

    // ---------- Öğrenci bağlandıktan sonra çağırır ----------
    // Kimlik istemciden alınmıyor, giriş token'ından okunuyor: kimse başkası adına bağlanamaz.
    public async Task RegisterStudent()
    {
        if (!Context.User!.IsInRole("Student"))
            throw new HubException("Bu işlem sadece öğrenciler içindir.");

        int studentId = int.Parse(Context.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        string studentName = Context.User.FindFirstValue("full_name") ?? Context.User.Identity?.Name ?? "";

        var s = new ConnectedStudentDto(Context.ConnectionId, studentId, studentName, ClientIp, DateTime.UtcNow);
        _session.AddStudent(s);
        await Groups.AddToGroupAsync(Context.ConnectionId, StudentsGroup);

        await _logger.LogAsync("CONNECT", $"Öğrenci={studentId} ({studentName}) IP={s.Ip}");
        await Clients.Group(InstructorsGroup).SendAsync(HubEvents.StudentListChanged, _session.GetStudents());

        // Sınav başladıktan sonra bağlanan / kopup geri gelen öğrenciye aktif sınavı gönder
        if (_session.ActivePayload is { } active && DateTime.UtcNow < active.EndUtc)
            await Clients.Caller.SendAsync(HubEvents.ReceiveExam, active);
    }

    // ---------- Eğitmen arayüzü bağlanınca çağırır ----------
    public async Task RegisterInstructor()
    {
        // Öğrenciler eğitmen grubuna girip canlı sonuçları izleyemesin
        if (!Context.User!.IsInRole("Instructor"))
            throw new HubException("Yetkisiz.");

        await Groups.AddToGroupAsync(Context.ConnectionId, InstructorsGroup);
        await Clients.Caller.SendAsync(HubEvents.StudentListChanged, _session.GetStudents());
    }

    // ---------- Vize İsteri 6 + 7: cevap gönderimi ----------
    public async Task<SubmitResultDto> SubmitExam(SubmitExamDto dto)
    {
        var now = DateTime.UtcNow;

        // GÜVENLİK: öğrenci kimliği istemciden değil, bu bağlantının kaydından alınır.
        var me = _session.GetStudent(Context.ConnectionId);
        if (me is null)
        {
            await _logger.LogAsync("REJECTED", $"Kayıtsız bağlantı IP={ClientIp} Sınav={dto.ExamId}");
            return new SubmitResultDto(false, "Önce giriş yapmalısınız.");
        }

        if (!_session.IsSubmissionAllowed(dto.ExamId, now, out var reason))
        {
            await _logger.LogAsync("REJECTED", $"Öğrenci={me.StudentId} Sınav={dto.ExamId} IP={ClientIp} Sebep={reason}");
            return new SubmitResultDto(false, reason);
        }

        try
        {
            var (sub, result) = await _grading.GradeAndSave(dto, me.StudentId, me.StudentName, ClientIp);
            await _logger.LogAsync("SUBMIT",
                $"Öğrenci={me.StudentId} ({me.StudentName}) Sınav={dto.ExamId} IP={ClientIp} Puan={result.TotalScore}/{result.MaxScore} KayıtId={sub.Id}");

            // Eğitmen ekranı canlı güncellensin
            await Clients.Group(InstructorsGroup).SendAsync(HubEvents.SubmissionReceived,
                new SubmissionNoticeDto(me.StudentId, me.StudentName, dto.ExamId, result.TotalScore, result.MaxScore, sub.SubmittedAt));

            return new SubmitResultDto(true, "Cevaplarınız alındı.", result.TotalScore, result.MaxScore);
        }
        catch (InvalidOperationException ex) // ör. ikinci gönderim
        {
            await _logger.LogAsync("REJECTED", $"Öğrenci={me.StudentId} Sınav={dto.ExamId} Sebep={ex.Message}");
            return new SubmitResultDto(false, ex.Message);
        }
    }

    // ---------- Kopma yönetimi ----------
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var s = _session.RemoveStudent(Context.ConnectionId);
        if (s is not null)
        {
            await _logger.LogAsync("DISCONN", $"Öğrenci={s.StudentId} ({s.StudentName}) IP={s.Ip} Hata={exception?.Message ?? "-"}");
            await Clients.Group(InstructorsGroup).SendAsync(HubEvents.StudentListChanged, _session.GetStudents());
        }
        await base.OnDisconnectedAsync(exception);
    }
}