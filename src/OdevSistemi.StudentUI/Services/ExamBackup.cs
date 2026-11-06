using System.Text.Json;


namespace OdevSistemi.StudentUI;

///Sınav sırasında diske yazılan yedek. Uygulama kapanırsa kaldığı yerden devam edilir.
public class ExamBackupData
{
    public int UserId { get; set; }
    public int ExamId { get; set; }
    public DateTime EndAtUtc { get; set; }
    public int TotalSec { get; set; }
    public int Current { get; set; }
    public ExamPackage Exam { get; set; } = new();
    public Dictionary<int, string> Answers { get; set; } = new();
    public List<int> Flags { get; set; } = new();
}


/// Cevapları bu bilgisayarda küçük bir JSON dosyasına yazar.
/// Konum: %LocalAppData%\OgrenciSinav\yedek_{kullaniciId}.json
/// Dosya, sunucu cevapları kabul edince silinir. Hiçbir hata uygulamayı durdurmaz.

public static class ExamBackup
{
    private static readonly string Dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OgrenciSinav");

    private static string PathFor(int userId) => Path.Combine(Dir, $"yedek_{userId}.json");

    public static void Save(ExamBackupData data)
    {
        try
        {
            Directory.CreateDirectory(Dir);
            var path = PathFor(data.UserId);
            var tmp = path + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(data));
            File.Move(tmp, path, overwrite: true);   // yarım dosya kalmasın diye önce geçici dosyaya yazılır
        }
        catch { /* yedek alınamasa da sınav devam etmeli */ }
    }

    public static ExamBackupData? Load(int userId)
    {
        try
        {
            var path = PathFor(userId);
            if (!File.Exists(path)) return null;
            var data = JsonSerializer.Deserialize<ExamBackupData>(File.ReadAllText(path));
            return data is { Exam.Questions.Count: > 0 } ? data : null;
        }
        catch { return null; }
    }

    /// Sunucu cevapları aldı: yedeğe artık gerek yok.
    public static void Delete(int userId)
    {
        try { File.Delete(PathFor(userId)); } catch { }
    }

    /// Sunucu gönderimi kesin olarak reddetti (süre bitmiş vb.): veri kaybolmasın diye dosya yeniden adlandırılır.
    public static void Archive(int userId, int examId)
    {
        try
        {
            var path = PathFor(userId);
            if (!File.Exists(path)) return;
            var target = Path.Combine(Dir, $"yedek_{userId}_sinav{examId}_gonderilemedi_{DateTime.Now:yyyyMMdd_HHmmss}.json");
            File.Move(path, target, overwrite: true);
        }
        catch { }
    }
}
