using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using OdevSistemi.InstructorUI.Models;
using OdevSistemi.InstructorUI.Services;

namespace OdevSistemi.InstructorUI.Views;

public partial class SoruBankasiView : UserControl
{
    private const string Tumu = "Tümü";
    private bool _yukleniyor;

    private Window? AnaPencere => TopLevel.GetTopLevel(this) as Window;

    public SoruBankasiView()
    {
        InitializeComponent();
        _ = Calistir(YukleAsync);
    }

    // Kategorileri ve tabloyu baştan yükler, seçili filtreyi korur
    private async Task YukleAsync()
    {
        _yukleniyor = true;
        var onceki = KategoriFiltresi.SelectedItem as string ?? Tumu;
        var liste = new List<string> { Tumu };
        liste.AddRange(await Servis.Aktif.KategorileriGetirAsync());
        KategoriFiltresi.ItemsSource = liste;
        KategoriFiltresi.SelectedItem = liste.Contains(onceki) ? onceki : Tumu;
        _yukleniyor = false;

        await TabloyuYenileAsync();
    }

    private async Task TabloyuYenileAsync()
    {
        var secili = KategoriFiltresi.SelectedItem as string;
        var sorular = await Servis.Aktif.SorulariGetirAsync(secili is null || secili == Tumu ? null : secili);
        SoruTablosu.ItemsSource = sorular;
        DurumMetni.Text = $"{sorular.Count} soru listeleniyor";
    }

    private async void KategoriFiltresi_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!_yukleniyor) await Calistir(TabloyuYenileAsync);
    }

    private async void YeniSoru_Click(object? sender, RoutedEventArgs e) => await Calistir(async () =>
    {
        if (AnaPencere is null) return;
        var form = new SoruFormWindow(null, await Servis.Aktif.KategorileriGetirAsync());
        var yeni = await form.ShowDialog<Soru?>(AnaPencere);
        if (yeni is null) return;

        await Servis.Aktif.SoruEkleAsync(yeni);
        await YukleAsync();
        DurumMetni.Text = "Soru eklendi.";
    });

    private async void Duzenle_Click(object? sender, RoutedEventArgs e) => await Calistir(SeciliyiDuzenleAsync);

    private async void SoruTablosu_DoubleTapped(object? sender, TappedEventArgs e) => await Calistir(SeciliyiDuzenleAsync);

    private async Task SeciliyiDuzenleAsync()
    {
        if (SoruTablosu.SelectedItem is not Soru secili)
        {
            DurumMetni.Text = "Önce tablodan bir soru seçin.";
            return;
        }
        if (AnaPencere is null) return;

        var form = new SoruFormWindow(secili, await Servis.Aktif.KategorileriGetirAsync());
        var guncel = await form.ShowDialog<Soru?>(AnaPencere);
        if (guncel is null) return;

        await Servis.Aktif.SoruGuncelleAsync(guncel);
        await YukleAsync();
        DurumMetni.Text = "Soru güncellendi.";
    }

    private async void Sil_Click(object? sender, RoutedEventArgs e) => await Calistir(async () =>
    {
        if (SoruTablosu.SelectedItem is not Soru secili)
        {
            DurumMetni.Text = "Önce tablodan bir soru seçin.";
            return;
        }

        var kisa = secili.Metin.Length > 60 ? secili.Metin[..60] + "..." : secili.Metin;
        if (!await Diyalog.OnaylaAsync(AnaPencere, $"\"{kisa}\" sorusu silinsin mi?", "Soruyu Sil"))
            return;

        await Servis.Aktif.SoruSilAsync(secili.Id);
        await YukleAsync();
        DurumMetni.Text = "Soru silindi.";
    });

    // Sunucu hatası olursa uygulama çökmesin, durum satırında gösterilsin
    private async Task Calistir(Func<Task> islem)
    {
        try { await islem(); }
        catch (Exception ex) { DurumMetni.Text = "Hata: " + ex.Message; }
    }
}