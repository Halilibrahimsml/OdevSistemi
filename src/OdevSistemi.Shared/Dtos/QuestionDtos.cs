namespace OdevSistemi.Shared.Dtos;

/// <summary>Sunucunun eğitmene döndüğü soru bilgisi (cevap anahtarı dahil).</summary>
public class QuestionDto
{
    public int Id { get; set; }
    public QuestionType Type { get; set; }
    public string Category { get; set; } = "";
    public string Text { get; set; } = "";
    public List<string> Options { get; set; } = new();
    /// <summary>Çoktan seçmeli: doğru şıkkın numarası ("0" = A, "1" = B ...). Klasik: kabul edilen cevaplar ("printf|printf()").</summary>
    public string CorrectAnswer { get; set; } = "";
    public double DefaultPoints { get; set; }
}

/// <summary>Soru eklerken veya güncellerken eğitmen arayüzünün gönderdiği bilgi.</summary>
public class QuestionSaveRequest
{
    public QuestionType Type { get; set; }
    public string Category { get; set; } = "";
    public string Text { get; set; } = "";
    public List<string> Options { get; set; } = new();
    public string CorrectAnswer { get; set; } = "";
    public double DefaultPoints { get; set; } = 10;
}
