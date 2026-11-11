using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using OdevSistemi.InstructorUI.Models;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace OdevSistemi.InstructorUI.Services;

// "1. ... A) ... B) ..." biçimindeki soruları ve sondaki cevap anahtarını okur
public static class PdfSoruOkuyucu
{
    private const RegexOptions Ayar = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
    private const string Harfler = "ABCDEF";

    // "1. metin", "1) metin", "1- metin", "Soru 1: metin", "1. Soru: metin"
    private static readonly Regex SoruBasi =
        new(@"^\s*(?:soru\s*)?(\d{1,3})\s*[.):\-]\s*(?:soru\b\s*[.):\-]?\s*)?(.*)$", Ayar);

    // "CEVAP ANAHTARI", "Cevap Anahtarı:", "Cevaplar"
    private static readonly Regex AnahtarBasligi =
        new(@"^\s*(?:cevap\s*anahtar\w*|cevaplar)\b\s*[:\-]?\s*(.*)$", Ayar);

    // Bir satırda bir ya da birden fazla şık cevabı: "1-B", "1. soru B", "1-B 2-C 3-A"
    private static readonly Regex CokluCevap =
        new(@"^(?:\s*(?:soru\s*)?\d{1,3}\s*[.):\-=]?\s*(?:soru\b\s*)?[.):\-=]?\s*[A-F]\b[\s,;]*)+$", Ayar);
    private static readonly Regex CevapParcasi =
        new(@"(?:soru\s*)?(\d{1,3})\s*[.):\-=]?\s*(?:soru\b\s*)?[.):\-=]?\s*([A-F])\b", Ayar);

    // Klasik soru cevabı gibi uzun tek cevap: "4. printf|printf()"
    private static readonly Regex TekCevap =
        new(@"^\s*(?:soru\s*)?(\d{1,3})\s*[.):\-=]\s*(?:soru\b\s*[.):\-=]?\s*)?(.+)$", Ayar);

    // Şık başlangıçları: "A)", "a)", "A." (önünde boşluk ya da satır başı olmalı)
    private static readonly Regex[] SikBasi =
        Harfler.Select(h => new Regex($@"(?<=^|\s){h}\s*[).]\s*", Ayar)).ToArray();

    private sealed class Blok
    {
        public int No;
        public List<string> Satirlar = new();
    }

    public static PdfOkumaSonucu Oku(byte[] pdf) => Ayristir(SatirlariOku(pdf));

    // ---------- 1. PDF'i satırlara çevir ----------

    private static List<string> SatirlariOku(byte[] pdf)
    {
        var satirlar = new List<string>();
        using var belge = PdfDocument.Open(pdf);

        foreach (Page sayfa in belge.GetPages())
        {
            // Kelimeleri yukarıdan aşağı, soldan sağa sırala; aynı yükseklikteki kelimeler aynı satırdır
            var kelimeler = sayfa.GetWords()
                .OrderByDescending(Taban)
                .ThenBy(k => k.BoundingBox.Left)
                .ToList();

            var satir = new List<Word>();
            double satirY = 0;
            foreach (var k in kelimeler)
            {
                double tolerans = Math.Max(2, k.BoundingBox.Height * 0.4);
                if (satir.Count > 0 && Math.Abs(Taban(k) - satirY) > tolerans)
                {
                    satirlar.Add(SatiriBirlestir(satir));
                    satir.Clear();
                }
                if (satir.Count == 0) satirY = Taban(k);
                satir.Add(k);
            }
            if (satir.Count > 0) satirlar.Add(SatiriBirlestir(satir));
        }
        return satirlar;
    }

    private static double Taban(Word k) =>
        k.Letters.Count > 0 ? k.Letters[0].StartBaseLine.Y : k.BoundingBox.Bottom;

    private static string SatiriBirlestir(List<Word> satir) =>
        string.Join(" ", satir.OrderBy(k => k.BoundingBox.Left).Select(k => k.Text));

    // ---------- 2. Satırlardan soruları ve cevapları çıkar ----------

    public static PdfOkumaSonucu Ayristir(List<string> hamSatirlar)
    {
        var sonuc = new PdfOkumaSonucu();
        var satirlar = hamSatirlar.Select(s => s.Trim()).Where(s => s.Length > 0).ToList();

        if (satirlar.Count == 0)
        {
            sonuc.Uyarilar.Add("PDF'te okunabilir metin bulunamadı. Taranmış (resim) bir PDF olabilir.");
            return sonuc;
        }

        // a) Cevap anahtarını sorulardan ayır (sondaki başlığı ara)
        int baslik = -1;
        for (int i = satirlar.Count - 1; i >= 0; i--)
        {
            var m = AnahtarBasligi.Match(satirlar[i]);
            if (m.Success && (m.Groups[1].Value.Trim() == "" || CevapSatiriMi(m.Groups[1].Value)))
            {
                baslik = i;
                break;
            }
        }

        List<string> soruSatirlari, anahtarSatirlari;
        if (baslik >= 0)
        {
            soruSatirlari = satirlar.Take(baslik).ToList();
            anahtarSatirlari = satirlar.Skip(baslik + 1).ToList();
            var ayniSatirdaki = AnahtarBasligi.Match(satirlar[baslik]).Groups[1].Value.Trim();
            if (ayniSatirdaki != "") anahtarSatirlari.Insert(0, ayniSatirdaki);
        }
        else
        {
            // Başlık yoksa: sondaki "1-B 2-C" gibi satırları cevap anahtarı say
            int i = satirlar.Count;
            while (i > 0 && CokluCevap.IsMatch(satirlar[i - 1])) i--;
            soruSatirlari = satirlar.Take(i).ToList();
            anahtarSatirlari = satirlar.Skip(i).ToList();
            if (anahtarSatirlari.Count == 0)
                sonuc.Uyarilar.Add("Cevap anahtarı bulunamadı. PDF'in sonuna 'Cevap Anahtarı' başlığı ekleyin " +
                                   "ya da cevapları satırlara çift tıklayıp elle girin.");
        }

        // b) Cevapları numaraya göre sözlüğe al
        var cevaplar = new Dictionary<int, string>();
        int? sonNo = null;
        foreach (var s in anahtarSatirlari)
        {
            if (CokluCevap.IsMatch(s))
            {
                foreach (Match m in CevapParcasi.Matches(s))
                {
                    sonNo = int.Parse(m.Groups[1].Value);
                    cevaplar[sonNo.Value] = m.Groups[2].Value;
                }
            }
            else if (TekCevap.Match(s) is { Success: true } m)
            {
                sonNo = int.Parse(m.Groups[1].Value);
                cevaplar[sonNo.Value] = m.Groups[2].Value.Trim();
            }
            else if (sonNo is not null)
            {
                cevaplar[sonNo.Value] += " " + s; // uzun klasik cevabın devam satırı
            }
        }

        // c) Soruları numaralarına göre böl (1, 2, 3... sırayla gitmeli)
        var bloklar = new List<Blok>();
        int beklenen = 1;
        foreach (var s in soruSatirlari)
        {
            var m = SoruBasi.Match(s);
            if (m.Success && int.Parse(m.Groups[1].Value) == beklenen)
            {
                var blok = new Blok { No = beklenen };
                blok.Satirlar.Add(m.Groups[2].Value);
                bloklar.Add(blok);
                beklenen++;
            }
            else if (bloklar.Count > 0)
            {
                bloklar[^1].Satirlar.Add(s);
            }
        }

        if (bloklar.Count == 0)
        {
            sonuc.Uyarilar.Add("Numaralı soru bulunamadı. Sorular '1.', '2.' şeklinde numaralandırılmalı.");
            return sonuc;
        }

        // d) Her soru için şıkları ayır ve cevabı eşleştir
        foreach (var b in bloklar)
        {
            var metin = Regex.Replace(string.Join(" ", b.Satirlar), @"\s+", " ").Trim();
            var (govde, siklar) = SiklariAyir(metin);
            var soru = new Soru { Metin = govde };
            var satir = new IceAktarilanSoru(b.No, soru);
            cevaplar.TryGetValue(b.No, out var cevap);

            if (siklar.Count >= 2)
            {
                soru.Tur = SoruTuru.CoktanSecmeli;
                soru.Secenekler = siklar;

                var c = (cevap ?? "").Trim().ToUpperInvariant();
                int index = c.Length > 0 ? Harfler.IndexOf(c[0]) : -1;
                bool tekHarf = c.Length == 1 || (c.Length > 1 && !char.IsLetterOrDigit(c[1]));

                if (cevap is null) satir.Hata = "Cevap anahtarında yok";
                else if (index < 0 || index >= siklar.Count || !tekHarf) satir.Hata = $"Geçersiz cevap: {cevap}";
                else soru.DogruSecenekIndex = index;

                if (siklar.Any(x => x == "")) satir.Hata = "Boş şık var";
            }
            else
            {
                soru.Tur = SoruTuru.Klasik;
                if (string.IsNullOrWhiteSpace(cevap)) satir.Hata = "Cevap anahtarında yok";
                else soru.KlasikCevapAnahtari = cevap.Trim();
            }

            if (govde == "") satir.Hata = "Soru metni boş";
            satir.Secili = satir.Hata is null;
            sonuc.Sorular.Add(satir);
        }

        var fazla = cevaplar.Keys.Where(n => n > bloklar.Count).OrderBy(n => n).ToList();
        if (fazla.Count > 0)
            sonuc.Uyarilar.Add($"Cevap anahtarında karşılığı bulunmayan numaralar: {string.Join(", ", fazla)}");

        return sonuc;
    }

    private static bool CevapSatiriMi(string s) => CokluCevap.IsMatch(s) || TekCevap.IsMatch(s);

    // "Soru metni A) x B) y C) z" -> ("Soru metni", [x, y, z])
    private static (string Govde, List<string> Siklar) SiklariAyir(string metin)
    {
        var baslar = new List<(int HarfKonumu, int IcerikBasi)>();
        int arama = 0;
        foreach (var r in SikBasi)
        {
            var m = r.Match(metin, arama);
            if (!m.Success) break;
            baslar.Add((m.Index, m.Index + m.Length));
            arama = m.Index + m.Length;
        }

        if (baslar.Count < 2) return (metin, new List<string>());

        var siklar = new List<string>();
        for (int i = 0; i < baslar.Count; i++)
        {
            int son = i + 1 < baslar.Count ? baslar[i + 1].HarfKonumu : metin.Length;
            siklar.Add(metin[baslar[i].IcerikBasi..son].Trim());
        }
        return (metin[..baslar[0].HarfKonumu].Trim(), siklar);
    }
}