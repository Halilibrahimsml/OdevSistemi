using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OdevSistemi.InstructorUI.Models;

namespace OdevSistemi.InstructorUI.Services;

public class SahteEgitmenServisi : IEgitmenServisi
{
    private readonly List<Soru> _sorular = new()
    {
        new Soru { Id = 1, Metin = "C dilinde int tipi çoğu sistemde kaç byte yer kaplar?",
                   Tur = SoruTuru.CoktanSecmeli, Kategori = "Veri Tipleri",
                   Secenekler = new() { "1", "2", "4", "8" }, DogruSecenekIndex = 2 },
        new Soru { Id = 2, Metin = "Pointer nedir? Kısaca açıklayınız.",
                   Tur = SoruTuru.Klasik, Kategori = "Pointerlar",
                   KlasikCevapAnahtari = "bellek adresi" },
        new Soru { Id = 3, Metin = "Hangisi bir döngü deyimi değildir?",
                   Tur = SoruTuru.CoktanSecmeli, Kategori = "Kontrol Yapıları",
                   Secenekler = new() { "for", "while", "switch", "do-while" }, DogruSecenekIndex = 2 },
    };
    private readonly List<Sinav> _sinavlar = new();
    private readonly List<string> _sistemLogu = new();
    private int _sonrakiSoruId = 4, _sonrakiSinavId = 1;
    private readonly Random _r = new();

    private static string Zaman() => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

    public Task<bool> GirisYapAsync(string k, string s) =>
        Task.FromResult(k == "egitmen" && s == "1234");

    public Task<List<string>> KategorileriGetirAsync() =>
        Task.FromResult(_sorular.Select(x => x.Kategori).Distinct().OrderBy(x => x).ToList());

    public Task<List<Soru>> SorulariGetirAsync(string? kategori = null) =>
        Task.FromResult(kategori is null ? _sorular.ToList()
                                         : _sorular.Where(x => x.Kategori == kategori).ToList());

    public Task SoruEkleAsync(Soru soru) { soru.Id = _sonrakiSoruId++; _sorular.Add(soru); return Task.CompletedTask; }

    public Task SoruGuncelleAsync(Soru soru)
    {
        var i = _sorular.FindIndex(x => x.Id == soru.Id);
        if (i >= 0) _sorular[i] = soru;
        return Task.CompletedTask;
    }

    public Task SoruSilAsync(int id) { _sorular.RemoveAll(x => x.Id == id); return Task.CompletedTask; }

    public Task<Sinav> SinavOlusturAsync(Sinav sinav)
    {
        sinav.Id = _sonrakiSinavId++;
        _sinavlar.Add(sinav);
        return Task.FromResult(sinav);
    }

    public Task<List<Sinav>> SinavlariGetirAsync() => Task.FromResult(_sinavlar.ToList());

    public Task<List<BagliOgrenci>> BagliOgrencileriGetirAsync()
    {
        var hepsi = new List<BagliOgrenci>
        {
            new() { OgrenciNo = "220201001", AdSoyad = "Ali Yılmaz",   IpAdresi = "192.168.1.21" },
            new() { OgrenciNo = "220201002", AdSoyad = "Ayşe Demir",   IpAdresi = "192.168.1.22" },
            new() { OgrenciNo = "220201003", AdSoyad = "Mehmet Kaya",  IpAdresi = "192.168.1.23" },
            new() { OgrenciNo = "220201004", AdSoyad = "Zeynep Çelik", IpAdresi = "192.168.1.24" },
            new() { OgrenciNo = "220201005", AdSoyad = "Can Arslan",   IpAdresi = "192.168.1.25" },
        };
        foreach (var o in hepsi) o.BaglanmaZamani = DateTime.Now.AddMinutes(-_r.Next(1, 20));
        return Task.FromResult(hepsi.Take(_r.Next(2, hepsi.Count + 1)).ToList());
    }

    public Task SinavGonderAsync(int sinavId)
    {
        var s = _sinavlar.First(x => x.Id == sinavId);
        _sistemLogu.Add($"[{Zaman()}] '{s.Ad}' sınavı gönderildi ({s.Sorular.Count} soru, {s.SureDakika} dk)");
        return Task.CompletedTask;
    }

    public Task<List<SinavSonucu>> SonuclariGetirAsync(int sinavId)
    {
        var sinav = _sinavlar.FirstOrDefault(x => x.Id == sinavId);
        int toplamSoru = sinav?.Sorular.Count ?? 0;
        int toplamPuan = sinav?.Sorular.Sum(x => x.Puan) ?? 0;

        var sonuc = new List<SinavSonucu>();
        foreach (var o in BagliOgrencileriGetirAsync().Result)
        {
            var dogrular = sinav?.Sorular.Where(_ => _r.Next(2) == 0).ToList() ?? new();
            sonuc.Add(new SinavSonucu
            {
                OgrenciNo = o.OgrenciNo, AdSoyad = o.AdSoyad,
                DogruSayisi = dogrular.Count, ToplamSoru = toplamSoru,
                Puan = toplamPuan == 0 ? 0 : Math.Round(100.0 * dogrular.Sum(x => x.Puan) / toplamPuan, 1),
                TeslimZamani = DateTime.Now.AddMinutes(-_r.Next(0, 30)),
            });
        }
        return Task.FromResult(sonuc);
    }

    public Task<List<string>> LogDosyalariGetirAsync() =>
        Task.FromResult(new List<string> { "teslimler.log", "sistem.log" });

    public Task<string> LogIcerigiGetirAsync(string dosyaAdi) => Task.FromResult(dosyaAdi switch
    {
        "teslimler.log" => string.Join(Environment.NewLine,
            "[2026-11-05 10:02:13] 220201001 - Ali Yılmaz - Sınav teslim edildi",
            "[2026-11-05 10:05:47] 220201002 - Ayşe Demir - Sınav teslim edildi",
            "[2026-11-05 10:31:02] 220201003 - Mehmet Kaya - Süre doldu, teslim reddedildi"),
        "sistem.log" => _sistemLogu.Count == 0 ? "(henüz kayıt yok)" : string.Join(Environment.NewLine, _sistemLogu),
        _ => ""
    });
}