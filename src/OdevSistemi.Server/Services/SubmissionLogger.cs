using System.Text;

namespace OdevSistemi.Server.Services;

/// <summary>
/// Vize İsteri 6 / Hedef 12: gönderimleri eğitmen bilgisayarında fiziksel .log dosyasına yazar.
/// Dosya: &lt;sunucu klasörü&gt;/Logs/submissions-2026-11-05.log  (her gün ayrı dosya)
/// </summary>
public class SubmissionLogger
{
    private readonly string _dir;
    private readonly SemaphoreSlim _gate = new(1, 1); // aynı anda 40 yazma gelirse dosya bozulmasın

    public SubmissionLogger(IWebHostEnvironment env)
    {
        _dir = Path.Combine(env.ContentRootPath, "Logs");
        Directory.CreateDirectory(_dir);
    }

    private string FileFor(DateTime localDate) => Path.Combine(_dir, $"submissions-{localDate:yyyy-MM-dd}.log");

    public async Task LogAsync(string level, string message)
    {
        var now = DateTime.Now; // eğitmen bilgisayarının yerel saati
        var line = $"{now:yyyy-MM-dd HH:mm:ss.fff} | {level,-8} | {message}{Environment.NewLine}";
        await _gate.WaitAsync();
        try { await File.AppendAllTextAsync(FileFor(now), line, Encoding.UTF8); }
        finally { _gate.Release(); }
    }

    public IEnumerable<string> ListLogFiles()
        => Directory.GetFiles(_dir, "*.log").Select(f => Path.GetFileName(f)).OrderDescending();

    public async Task<string[]> ReadLogAsync(string fileName)
    {
        // Path traversal engeli: "../../x" gibi isimler reddedilir
        var path = Path.Combine(_dir, Path.GetFileName(fileName));
        if (!File.Exists(path)) return Array.Empty<string>();

        await _gate.WaitAsync();
        try { return await File.ReadAllLinesAsync(path, Encoding.UTF8); }
        finally { _gate.Release(); }
    }
}
