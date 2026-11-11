using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using OdevSistemi.InstructorUI.Models;

namespace OdevSistemi.InstructorUI.Views;

public partial class PdfIceAktarWindow : Window
{
    private readonly List<IceAktarilanSoru> _sorular;
    private readonly List<string> _uyarilar;
    private readonly List<string> _kategoriler;

    public PdfIceAktarWindow() : this("", new PdfOkumaSonucu(), new List<string>()) { }

    public PdfIceAktarWindow(string dosyaAdi, PdfOkumaSonucu sonuc, List<string> kategoriler)
    {
        InitializeComponent();
        _sorular = sonuc.Sorular;
        _uyarilar = sonuc.Uyarilar;
        _kategoriler = kategoriler;

        BaslikMetni.Text = $"PDF'den Soru Aktar: {dosyaAdi}";
        KategoriKutusu.ItemsSource = kategoriler;
        KategoriKutusu.Text = dosyaAdi;
        Tablo.ItemsSource = _sorular;

        foreach (var s in _sorular) s.PropertyChanged += (_, _) => OzetiGuncelle();
        OzetiGuncelle();
    }

    private string Kategori => KategoriKutusu.Text?.Trim() ?? "";
    private int Puan => (int)(PuanKutusu.Value ?? 10);

    private void OzetiGuncelle()
    {
        int hazir = _sorular.Count(s => s.Gecerli);
        int secili = _sorular.Count(s => s.Secili && s.Gecerli);
        var metin = $"{_sorular.Count} soru bulundu  •  {hazir} hazır  •  {_sorular.Count - hazir} düzeltilmeli  " +
                    $"•  {secili} seçili. Bir satırı düzeltmek için üzerine çift tıklayın.";
        if (_uyarilar.Count > 0)
            metin += "\n" + string.Join("\n", _uyarilar.Select(u => "Uyarı: " + u));
        OzetMetni.Text = metin;
    }

    // Hatalı (veya yanlış okunmuş) satırı mevcut soru formuyla düzelt
    private async void Tablo_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (Tablo.SelectedItem is not IceAktarilanSoru satir) return;

        var taslak = satir.Soru;
        if (!satir.Duzenlendi)
        {
            taslak.Kategori = Kategori == "" ? "Genel" : Kategori;
            taslak.Puan = Puan;
        }

        var form = new SoruFormWindow(taslak, _kategoriler);
        var duzeltilmis = await form.ShowDialog<Soru?>(this);
        if (duzeltilmis is null) return;

        satir.Duzenlendi = true;
        satir.Soru = duzeltilmis;
        satir.Hata = null;
        satir.Secili = true;
    }

    private void Ekle_Click(object? sender, RoutedEventArgs e)
    {
        if (Kategori == "")
        {
            HataMesaji.Text = "Kategori girin.";
            return;
        }

        var secilenler = _sorular.Where(s => s.Secili && s.Gecerli).ToList();
        if (secilenler.Count == 0)
        {
            HataMesaji.Text = "Eklenecek soru seçilmedi.";
            return;
        }

        // Elle düzeltilmeyenlere üstteki kategori ve puanı uygula
        foreach (var s in secilenler.Where(s => !s.Duzenlendi))
        {
            s.Soru.Kategori = Kategori;
            s.Soru.Puan = Puan;
        }

        Close(secilenler.Select(s => s.Soru).ToList());
    }

    private void Vazgec_Click(object? sender, RoutedEventArgs e) => Close(null);
}