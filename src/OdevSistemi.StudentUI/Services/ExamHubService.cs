using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using OdevSistemi.Shared;
using OdevSistemi.Shared.Contracts;

namespace OdevSistemi.StudentUI;


/// SignalR (ExamHub) bağlantısı: öğrenciyi sunucuya kaydeder, sınav paketini yakalar,
/// cevapları gönderir ve bağlantı durumunu bildirir.
/// Adres ve metot adları OdevSistemi.Shared'dan gelir (ServerInfo, HubEvents, HubMethods).

public class ExamHubService
{
    private HubConnection? _conn;
    private int _userId;
    private string _userName = "";

    public event Action<ExamPackage>? ExamReceived;
    public event Action? ExamEnded;
    ///"connected", "reconnecting" veya "closed"
    public event Action<string>? StateChanged;

    public async Task ConnectAsync(string baseUrl, string token, int userId, string userName)
    {
        await DisconnectAsync();
        _userId = userId;
        _userName = userName;

        var conn = new HubConnectionBuilder()
            .WithUrl(baseUrl + ServerInfo.HubPath, options =>   // http://IP:5000/hubs/exam
            {
                options.AccessTokenProvider = () => Task.FromResult<string?>(token);
                options.SkipNegotiation = true;
                options.Transports = HttpTransportType.WebSockets;
            })
            .WithAutomaticReconnect(new[]
            {
                TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(20)
            })
            .Build();

        // Sunucu -> öğrenci: sınav paketi (doğru cevaplar sunucuda çıkarılmış olarak gelir)
        conn.On<StudentExamDto>(HubEvents.ReceiveExam, dto => ExamReceived?.Invoke(ExamPackage.FromDto(dto)));
        // Sunucu -> öğrenci: eğitmen sınavı bitirdi (parametresiz)
        conn.On(HubEvents.ExamEnded, () => ExamEnded?.Invoke());

        conn.Reconnecting += _ => { StateChanged?.Invoke("reconnecting"); return Task.CompletedTask; };
        conn.Reconnected += async _ =>
        {
            // Yeniden bağlanınca ConnectionId değişir; sunucu bizi tanısın diye tekrar kayıt olmalıyız.
            // Aksi halde SubmitExam "Önce giriş yapmalısınız." diye reddeder.
            try { await conn.InvokeAsync(HubMethods.RegisterStudent); } catch { }
            StateChanged?.Invoke("connected");
        };
        conn.Closed += _ => { StateChanged?.Invoke("closed"); return Task.CompletedTask; };

        _conn = conn;
        await conn.StartAsync();

        // Öğrenciyi sunucuya tanıt. Kimlik (Id, ad) sunucuda giriş token'ından okunur, parametre gönderilmez.
        // Sınav zaten başlamışsa sunucu paketi hemen geri gönderir.
        await conn.InvokeAsync(HubMethods.RegisterStudent);
    }

    
    /// Cevapları hub üzerinden gönderir.
    /// null dönerse sunucuya ulaşılamadı demektir (tekrar denenebilir).
    /// Success=false dönerse sunucu gönderimi kesin olarak reddetti (Message içinde sebep).
  
    public async Task<SubmitResultDto?> SubmitAsync(SubmitExamDto dto)
    {
        var conn = _conn;
        if (conn is null || conn.State != HubConnectionState.Connected) return null;
        try
        {
            return await conn.InvokeAsync<SubmitResultDto>(HubMethods.SubmitExam, dto);
        }
        catch
        {
            return null;
        }
    }

    public async Task DisconnectAsync()
    {
        var conn = _conn;
        if (conn is null) return;
        _conn = null;
        try { await conn.StopAsync(); } catch { /* yoksay */ }
        await conn.DisposeAsync();
    }
}