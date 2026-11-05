using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;


namespace OdevSistemi.StudentUI;

/// <summary>SignalR (ExamHub) bağlantısı: sınav paketini yakalar, bağlantı durumunu bildirir.</summary>
public class ExamHubService
{
    private HubConnection? _conn;

    public event Action<ExamPackage>? ExamReceived;
    public event Action? ExamEnded;
    /// <summary>"connected", "reconnecting" veya "closed"</summary>
    public event Action<string>? StateChanged;

    public async Task ConnectAsync(string baseUrl, string token, int userId, string userName)
    {
        _conn = new HubConnectionBuilder()
            .WithUrl(baseUrl + AppConfig.HubPath, options =>
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

        _conn.On<ExamPackage>(AppConfig.EventReceiveExam, exam => ExamReceived?.Invoke(exam));
        _conn.On(AppConfig.EventExamEnded, () => ExamEnded?.Invoke());

        _conn.Reconnecting += _ => { StateChanged?.Invoke("reconnecting"); return Task.CompletedTask; };
        _conn.Reconnected += _ => { StateChanged?.Invoke("connected"); return Task.CompletedTask; };
        _conn.Closed += _ => { StateChanged?.Invoke("closed"); return Task.CompletedTask; };

        await _conn.StartAsync();

        if (AppConfig.RegisterMethod is not null)
            await _conn.InvokeAsync(AppConfig.RegisterMethod, userId, userName);
    }

    public async Task DisconnectAsync()
    {
        if (_conn is null) return;
        try { await _conn.StopAsync(); } catch { /* yoksay */ }
        await _conn.DisposeAsync();
        _conn = null;
    }
}
