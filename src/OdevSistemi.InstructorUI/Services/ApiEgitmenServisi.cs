using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using OdevSistemi.InstructorUI.Models;
using OdevSistemi.Shared;
using OdevSistemi.Shared.Contracts;
using OdevSistemi.Shared.Dtos;

namespace OdevSistemi.InstructorUI.Services;

public class ApiEgitmenServisi : IEgitmenServisi
{
    private readonly HttpClient _http;

    public ApiEgitmenServisi(string sunucuAdresi)
    {
        _http = new HttpClient { BaseAddress = new Uri(sunucuAdresi), Timeout = TimeSpan.FromSeconds(10) };
    }

    // ---------------- Giriş (1. kişi) ----------------

    public async Task<bool> GirisYapAsync(string kullaniciAdi, string sifre)
    {
        var istek = new LoginRequest
        {
            Username = kullaniciAdi,
            Password = sifre,
            RequiredRole = UserRole.Instructor   // öğrenci hesabı eğitmen uygulamasına giremesin
        };

        var yanit = await _http.PostAsJsonAsync("api/auth/login", istek);
        if (yanit.StatusCode == HttpStatusCode.Unauthorized) return false;
        await HataVarsaFirlat(yanit);

        var giris = await yanit.Content.ReadFromJsonAsync<LoginResponse>()
                    ?? throw new Exception("Sunucudan boş cevap geldi.");

        // Bundan sonraki tüm isteklerde token gönderilir
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", giris.Token);
        return true;
    }

    // ---------------- Soru bankası (1. kişi) ----------------

    public Task<List<string>> KategorileriGetirAsync() =>
        GetAsync<List<string>>("api/questions/categories");

    public async Task<List<Soru>> SorulariGetirAsync(string? kategori = null)
    {
        var adres = kategori is null ? "api/questions" : $"api/questions?category={Uri.EscapeDataString(kategori)}";
        var liste = await GetAsync<List<QuestionDto>>(adres);
        return liste.Select(SoruyaCevir).ToList();
    }

    public async Task SoruEkleAsync(Soru soru) =>
        await HataVarsaFirlat(await _http.PostAsJsonAsync("api/questions", IstegeCevir(soru)));

    public async Task SoruGuncelleAsync(Soru soru) =>
        await HataVarsaFirlat(await _http.PutAsJsonAsync($"api/questions/{soru.Id}", IstegeCevir(soru)));

    public async Task SoruSilAsync(int id) =>
        await HataVarsaFirlat(await _http.DeleteAsync($"api/questions/{id}"));

    // ---------------- Sınav kaydı (1. kişi) ----------------

    public async Task<Sinav> SinavOlusturAsync(Sinav sinav)
    {
        var istek = new ExamSaveRequest
        {
            Title = sinav.Ad,
            DurationMinutes = sinav.SureDakika,
            Questions = sinav.Sorular
                .Select(s => new ExamQuestionItemRequest { QuestionId = s.SoruId, Points = s.Puan })
                .ToList()
        };

        var yanit = await _http.PostAsJsonAsync("api/exams", istek);
        await HataVarsaFirlat(yanit);
        var d = await yanit.Content.ReadFromJsonAsync<ExamDetailDto>()
                ?? throw new Exception("Sunucudan boş cevap geldi.");

        return new Sinav
        {
            Id = d.Id,
            Ad = d.Title,
            SureDakika = d.DurationMinutes,
            SoruSayisi = d.Questions.Count,
            Durum = d.Status,
            OlusturmaTarihi = YerelSaat(d.CreatedAt),
            Sorular = d.Questions
                .Select(q => new SinavSorusu { SoruId = q.Question.Id, Puan = (int)Math.Round(q.Points) })
                .ToList()
        };
    }

    public async Task<List<Sinav>> SinavlariGetirAsync()
    {
        var liste = await GetAsync<List<ExamSummaryDto>>("api/exams");
        return liste.Select(e => new Sinav
        {
            Id = e.Id,
            Ad = e.Title,
            SureDakika = e.DurationMinutes,
            SoruSayisi = e.QuestionCount,
            Durum = e.Status,
            OlusturmaTarihi = YerelSaat(e.CreatedAt)
        }).ToList();
    }

    // ---------------- Ağ, dağıtım, sonuç, log (2. kişi) ----------------

    public async Task<List<BagliOgrenci>> BagliOgrencileriGetirAsync()
    {
        var liste = await GetAsync<List<ConnectedStudentDto>>("api/exam/connected");
        return liste.Select(o => new BagliOgrenci
        {
            OgrenciNo = o.StudentId.ToString(),
            AdSoyad = o.StudentName,
            IpAdresi = o.Ip ?? "-",
            BaglanmaZamani = YerelSaat(o.ConnectedAtUtc)
        }).ToList();
    }

    public async Task SinavGonderAsync(int sinavId) =>
        await HataVarsaFirlat(await _http.PostAsync($"api/exam/{sinavId}/start", null));

    public async Task<List<SinavSonucu>> SonuclariGetirAsync(int sinavId)
    {
        var liste = await GetAsync<List<ExamResultRowDto>>($"api/exam/{sinavId}/results");
        return liste.Select(r => new SinavSonucu
        {
            OgrenciNo = r.StudentId.ToString(),
            AdSoyad = r.StudentName,
            Puan = r.TotalScore,
            MaxPuan = r.MaxScore,
            TeslimZamani = YerelSaat(r.SubmittedAtUtc),
            IpAdresi = r.ClientIp ?? "-"
        }).ToList();
    }

    public Task<List<string>> LogDosyalariGetirAsync() =>
        GetAsync<List<string>>("api/logs");

    public async Task<string> LogIcerigiGetirAsync(string dosyaAdi)
    {
        var satirlar = await GetAsync<string[]>($"api/logs/{Uri.EscapeDataString(dosyaAdi)}");
        return string.Join(Environment.NewLine, satirlar);
    }

    // ---------------- Yardımcılar ----------------

    private async Task<T> GetAsync<T>(string adres)
    {
        var yanit = await _http.GetAsync(adres);
        await HataVarsaFirlat(yanit);
        return await yanit.Content.ReadFromJsonAsync<T>()
               ?? throw new Exception("Sunucudan boş cevap geldi.");
    }

    // Sunucu {"message": "..."} döndürürse o mesajı ekranda göstermek için fırlatır
    private static async Task HataVarsaFirlat(HttpResponseMessage yanit)
    {
        if (yanit.IsSuccessStatusCode) return;
        if (yanit.StatusCode == HttpStatusCode.Unauthorized)
            throw new Exception("Oturum süresi doldu, lütfen tekrar giriş yapın.");

        string mesaj = $"Sunucu hatası ({(int)yanit.StatusCode})";
        try
        {
            var govde = await yanit.Content.ReadFromJsonAsync<JsonElement>();
            if (govde.ValueKind == JsonValueKind.Object && govde.TryGetProperty("message", out var m))
                mesaj = m.GetString() ?? mesaj;
        }
        catch { /* gövde JSON değilse varsayılan mesaj kalır */ }

        throw new Exception(mesaj);
    }

    // Sunucu saatleri UTC tutuyor, ekranda Türkiye saati görünsün
    private static DateTime YerelSaat(DateTime t) =>
        t.Kind == DateTimeKind.Local ? t : DateTime.SpecifyKind(t, DateTimeKind.Utc).ToLocalTime();

    private static Soru SoruyaCevir(QuestionDto q)
    {
        bool coktanSecmeli = q.Type == QuestionType.MultipleChoice;
        return new Soru
        {
            Id = q.Id,
            Metin = q.Text,
            Kategori = q.Category,
            Tur = coktanSecmeli ? SoruTuru.CoktanSecmeli : SoruTuru.Klasik,
            Secenekler = q.Options,
            DogruSecenekIndex = coktanSecmeli && int.TryParse(q.CorrectAnswer, out var i) ? i : -1,
            KlasikCevapAnahtari = coktanSecmeli ? null : q.CorrectAnswer,
            Puan = (int)Math.Round(q.DefaultPoints)
        };
    }

    private static QuestionSaveRequest IstegeCevir(Soru s)
    {
        bool coktanSecmeli = s.Tur == SoruTuru.CoktanSecmeli;
        return new QuestionSaveRequest
        {
            Type = coktanSecmeli ? QuestionType.MultipleChoice : QuestionType.Classic,
            Category = s.Kategori,
            Text = s.Metin,
            Options = coktanSecmeli ? s.Secenekler : new List<string>(),
            CorrectAnswer = coktanSecmeli ? s.DogruSecenekIndex.ToString() : s.KlasikCevapAnahtari ?? "",
            DefaultPoints = s.Puan
        };
    }
}