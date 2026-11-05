using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using OdevSistemi.InstructorUI.Models;

namespace OdevSistemi.InstructorUI.Views;

public partial class SoruFormWindow : Window
{
    private readonly Soru? _duzenlenen;

    private RadioButton[] Dogrular => new[] { DogruA, DogruB, DogruC, DogruD };
    private TextBox[] Secenekler => new[] { SecenekA, SecenekB, SecenekC, SecenekD };

    public SoruFormWindow() : this(null, new List<string>()) { } // tasarımcı için

    public SoruFormWindow(Soru? soru, List<string> kategoriler)
    {
        InitializeComponent();
        _duzenlenen = soru;
        KategoriKutusu.ItemsSource = kategoriler;

        if (soru is null)
        {
            TurSecimi.SelectedIndex = 0;
            DogruA.IsChecked = true;
            return;
        }

        // Düzenleme modu: mevcut değerleri forma doldur
        Title = "Soruyu Düzenle";
        BaslikMetni.Text = $"Soruyu Düzenle (#{soru.Id})";
        TurSecimi.SelectedIndex = soru.Tur == SoruTuru.CoktanSecmeli ? 0 : 1;
        MetinKutusu.Text = soru.Metin;
        KategoriKutusu.Text = soru.Kategori;
        PuanKutusu.Value = soru.Puan;
        CevapAnahtariKutusu.Text = soru.KlasikCevapAnahtari;
        for (int i = 0; i < 4; i++)
        {
            Secenekler[i].Text = i < soru.Secenekler.Count ? soru.Secenekler[i] : "";
            Dogrular[i].IsChecked = i == soru.DogruSecenekIndex;
        }
    }

    private void TurSecimi_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (SecenekPaneli is null || KlasikPaneli is null) return;
        bool coktanSecmeli = TurSecimi.SelectedIndex == 0;
        SecenekPaneli.IsVisible = coktanSecmeli;
        KlasikPaneli.IsVisible = !coktanSecmeli;
    }

    private void Kaydet_Click(object? sender, RoutedEventArgs e)
    {
        var metin = MetinKutusu.Text?.Trim() ?? "";
        var kategori = KategoriKutusu.Text?.Trim() ?? "";

        if (metin == "") { HataGoster("Soru metni boş olamaz."); return; }
        if (kategori == "") { HataGoster("Kategori boş olamaz."); return; }

        // Orijinal nesneyi değiştirmiyoruz; Vazgeç denirse liste bozulmasın
        var soru = new Soru
        {
            Id = _duzenlenen?.Id ?? 0,
            Metin = metin,
            Kategori = kategori,
            Puan = (int)(PuanKutusu.Value ?? 10),
            Tur = TurSecimi.SelectedIndex == 0 ? SoruTuru.CoktanSecmeli : SoruTuru.Klasik
        };

        if (soru.Tur == SoruTuru.CoktanSecmeli)
        {
            var siklar = Secenekler.Select(t => t.Text?.Trim() ?? "").ToList();
            if (siklar.Any(s => s == "")) { HataGoster("Dört şıkkın hepsini doldurun."); return; }

            int dogru = Array.FindIndex(Dogrular, r => r.IsChecked == true);
            if (dogru < 0) { HataGoster("Doğru şıkkı işaretleyin."); return; }

            soru.Secenekler = siklar;
            soru.DogruSecenekIndex = dogru;
        }
        else
        {
            var anahtar = CevapAnahtariKutusu.Text?.Trim() ?? "";
            if (anahtar == "") { HataGoster("Klasik soru için cevap anahtarı girin."); return; }
            soru.KlasikCevapAnahtari = anahtar;
        }

        Close(soru);
    }

    private void Vazgec_Click(object? sender, RoutedEventArgs e) => Close(null);

    private void HataGoster(string mesaj)
    {
        HataMesaji.Text = mesaj;
        HataMesaji.IsVisible = true;
    }
}