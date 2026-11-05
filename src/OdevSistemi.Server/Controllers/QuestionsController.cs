using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OdevSistemi.Server.Data;
using OdevSistemi.Server.Entities;
using OdevSistemi.Shared;
using OdevSistemi.Shared.Dtos;

namespace OdevSistemi.Server.Controllers;

/// <summary>
/// Soru Bankası API'si (sadece eğitmen kullanabilir).
///   GET    /api/questions                    → tüm sorular
///   GET    /api/questions?category=Diziler   → kategoriye göre
///   GET    /api/questions?search=dizi        → metinde arama
///   GET    /api/questions/categories         → kategori listesi
///   GET    /api/questions/5                  → tek soru
///   POST   /api/questions                    → yeni soru
///   PUT    /api/questions/5                  → soruyu güncelle
///   DELETE /api/questions/5                  → soruyu sil
/// </summary>
[ApiController]
[Route("api/questions")]
[Authorize(Roles = nameof(UserRole.Instructor))]
public class QuestionsController : ControllerBase
{
    private readonly AppDbContext _db;

    public QuestionsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<QuestionDto>>> GetAll([FromQuery] string? category, [FromQuery] string? search)
    {
        IQueryable<Question> query = _db.Questions.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(q => q.Category == category);

        if (!string.IsNullOrWhiteSpace(search))
        {
            string pattern = $"%{search.Trim()}%";
            query = query.Where(q => EF.Functions.Like(q.Text, pattern));
        }

        List<Question> questions = await query.OrderBy(q => q.Category).ThenBy(q => q.Id).ToListAsync();
        return questions.Select(ToDto).ToList();
    }

    [HttpGet("categories")]
    public async Task<ActionResult<List<string>>> GetCategories()
    {
        return await _db.Questions
            .Select(q => q.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<QuestionDto>> GetById(int id)
    {
        Question? question = await _db.Questions.FindAsync(id);
        if (question is null)
            return NotFound(new { message = "Soru bulunamadı." });

        return ToDto(question);
    }

    [HttpPost]
    public async Task<ActionResult<QuestionDto>> Create(QuestionSaveRequest request)
    {
        string? error = Validate(request);
        if (error is not null)
            return BadRequest(new { message = error });

        var question = new Question();
        Apply(question, request);

        _db.Questions.Add(question);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = question.Id }, ToDto(question));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<QuestionDto>> Update(int id, QuestionSaveRequest request)
    {
        Question? question = await _db.Questions.FindAsync(id);
        if (question is null)
            return NotFound(new { message = "Soru bulunamadı." });

        string? error = Validate(request);
        if (error is not null)
            return BadRequest(new { message = error });

        Apply(question, request);
        await _db.SaveChangesAsync();

        return ToDto(question);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        Question? question = await _db.Questions.FindAsync(id);
        if (question is null)
            return NotFound(new { message = "Soru bulunamadı." });

        // Bir sınavda kullanılan soru silinemez, yoksa o sınav bozulur.
        bool usedInExam = await _db.ExamQuestions.AnyAsync(eq => eq.QuestionId == id);
        if (usedInExam)
            return Conflict(new { message = "Bu soru bir sınavda kullanılıyor. Önce sınavdan çıkarın." });

        _db.Questions.Remove(question);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ---------------- yardımcı metodlar ----------------

    /// <summary>Gelen soru bilgisini kontrol eder. Sorun yoksa null, varsa hata mesajı döner.</summary>
    private static string? Validate(QuestionSaveRequest r)
    {
        if (!Enum.IsDefined(r.Type)) return "Geçersiz soru tipi.";
        if (string.IsNullOrWhiteSpace(r.Text)) return "Soru metni boş olamaz.";
        if (string.IsNullOrWhiteSpace(r.Category)) return "Kategori boş olamaz.";
        if (r.DefaultPoints <= 0) return "Puan 0'dan büyük olmalı.";

        if (r.Type == QuestionType.MultipleChoice)
        {
            List<string> options = (r.Options ?? new()).Select(o => o?.Trim() ?? "").ToList();

            if (options.Count < 2) return "Çoktan seçmeli soruda en az 2 şık olmalı.";
            if (options.Count > 6) return "En fazla 6 şık girilebilir.";
            if (options.Any(o => o.Length == 0)) return "Şıklar boş bırakılamaz.";

            if (!int.TryParse(r.CorrectAnswer, out int index) || index < 0 || index >= options.Count)
                return $"Doğru cevap 0 ile {options.Count - 1} arasında bir şık numarası olmalı (0 = A, 1 = B ...).";
        }
        else if (string.IsNullOrWhiteSpace(r.CorrectAnswer))
        {
            return "Klasik soru için cevap anahtarı girilmeli.";
        }

        return null;
    }

    /// <summary>İstekteki bilgileri soru nesnesine kopyalar.</summary>
    private static void Apply(Question q, QuestionSaveRequest r)
    {
        q.Type = r.Type;
        q.Category = r.Category.Trim();
        q.Text = r.Text.Trim();
        q.DefaultPoints = r.DefaultPoints;

        if (r.Type == QuestionType.MultipleChoice)
        {
            q.Options = (r.Options ?? new()).Select(o => o.Trim()).ToList();
            q.CorrectAnswer = r.CorrectAnswer.Trim();
        }
        else
        {
            // Klasik soruda şık olmaz. Kabul edilen cevaplar "|" ile ayrılır: "printf | printf()" → "printf|printf()"
            q.Options = new List<string>();
            q.CorrectAnswer = string.Join("|",
                r.CorrectAnswer.Split('|').Select(s => s.Trim()).Where(s => s.Length > 0));
        }
    }

    private static QuestionDto ToDto(Question q) => new()
    {
        Id = q.Id,
        Type = q.Type,
        Category = q.Category,
        Text = q.Text,
        Options = q.Options.ToList(),
        CorrectAnswer = q.CorrectAnswer,
        DefaultPoints = q.DefaultPoints
    };
}
