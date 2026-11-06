using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using OdevSistemi.InstructorUI.Models;
using OdevSistemi.InstructorUI.Services;

namespace OdevSistemi.InstructorUI.Views;

public partial class SonuclarView : UserControl
{
    private bool _yukleniyor;

    public SonuclarView()
    {
        InitializeComponent();
        _ = Calistir(SinavlariYukleAsync);
    }

    private async Task SinavlariYukleAsync()
    {
        var oncekiId = (SinavSecimi.SelectedItem as Sinav)?.Id;
        var sinavlar = (await Servis.Aktif.SinavlariGetirAsync())
                       .OrderByDescending(s => s.OlusturmaTarihi).ToList();

        BosMesaji.IsVisible = sinavlar.Count == 0;
        IcerikPaneli.IsVisible = sinavlar.Count > 0;

        _yukleniyor = true;
        SinavSecimi.ItemsSource = sinavlar;
        SinavSecimi.SelectedItem = sinavlar.FirstOrDefault(s => s.Id == oncekiId) ?? sinavlar.FirstOrDefault();
        _yukleniyor = false;

        await SonuclariYukleAsync();
    }

    private async Task SonuclariYukleAsync()
    {
        if (SinavSecimi.SelectedItem is not Sinav sinav) return;
        BitirButonu.IsVisible = sinav.Durum == "Devam ediyor";

        var sonuclar = (await Servis.Aktif.SonuclariGetirAsync(sinav.Id))
                       .OrderByDescending(s => s.Puan).ToList();
        SonucTablosu.ItemsSource = sonuclar;

        // Sınıf bazlı özet
        TeslimSayisi.Text = sonuclar.Count.ToString();
        if (sonuclar.Count == 0)
        {
            Ortalama.Text = EnYuksek.Text = EnDusuk.Text = "-";
        }
        else
        {
            Ortalama.Text = sonuclar.Average(s => s.Puan).ToString("0.0");
            EnYuksek.Text = sonuclar.Max(s => s.Puan).ToString("0.#");
            EnDusuk.Text = sonuclar.Min(s => s.Puan).ToString("0.#");
        }

        DurumMetni.Text = $"{sinav.Ad}  •  {sinav.SoruSayisi} soru  •  {sinav.SureDakika} dk  " +
                          $"•  Son güncelleme {DateTime.Now:HH:mm:ss}";
    }

    private async void SinavSecimi_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!_yukleniyor) await Calistir(SonuclariYukleAsync);
    }

    private async void Yenile_Click(object? sender, RoutedEventArgs e) => await Calistir(SinavlariYukleAsync);
    private async void Bitir_Click(object? sender, RoutedEventArgs e) => await Calistir(async () =>
{
    if (SinavSecimi.SelectedItem is not Sinav sinav) return;

    var onay = await Diyalog.OnaylaAsync(TopLevel.GetTopLevel(this) as Window,
        $"'{sinav.Ad}' sınavı şimdi bitirilecek. Henüz teslim etmeyen öğrencilerin " +
        "ekranı kapanacak ve yeni gönderim kabul edilmeyecek. Onaylıyor musunuz?",
        "Sınavı Bitir");
    if (!onay) return;

    await Servis.Aktif.SinavBitirAsync();
    BitirButonu.IsVisible = false;
    await SinavlariYukleAsync();
    DurumMetni.Text = $"'{sinav.Ad}' sınavı bitirildi.";
});

    private async Task Calistir(Func<Task> islem)
    {
        try { await islem(); }
        catch (Exception ex) { DurumMetni.Text = "Hata: " + ex.Message; }
    }
}