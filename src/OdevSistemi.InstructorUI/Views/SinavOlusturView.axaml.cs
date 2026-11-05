using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using OdevSistemi.InstructorUI.Models;
using OdevSistemi.InstructorUI.Services;

namespace OdevSistemi.InstructorUI.Views;

public partial class SinavOlusturView : UserControl
{
    private const string Tumu = "Tümü";
    private List<SecilebilirSoru> _tumSorular = new();
    private readonly DispatcherTimer _zamanlayici = new() { Interval = TimeSpan.FromSeconds(3) };
    private int _bagliSayisi;
    private bool _yukleniyor;

    private Window? AnaPencere => TopLevel.GetTopLevel(this) as Window;

    public SinavOlusturView()
    {
        InitializeComponent();
        _zamanlayici.Tick += async (_, _) => await OgrencileriYenileAsync();
        _ = Calistir(YukleAsync);
    }

    // Ekran açıkken canlı liste çalışsın, başka menüye geçince dursun
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _zamanlayici.Start();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _zamanlayici.Stop();
    }

    private async Task YukleAsync()
    {
        var sorular = await Servis.Aktif.SorulariGetirAsync();
        _tumSorular = sorular.Select(s => new SecilebilirSoru(s)).ToList();
        foreach (var s in _tumSorular)
            s.PropertyChanged += (_, _) => OzetiGuncelle();

        _yukleniyor = true;
        var kategoriler = new List<string> { Tumu };
        kategoriler.AddRange(await Servis.Aktif.KategorileriGetirAsync());
        KategoriFiltresi.ItemsSource = kategoriler;
        KategoriFiltresi.SelectedIndex = 0;
        _yukleniyor = false;

        Filtrele();
        OzetiGuncelle();
        await OgrencileriYenileAsync();
    }

    // Filtre sadece görünümü değiştirir; seçimler _tumSorular içinde korunur
    private void Filtrele()
    {
        var kategori = KategoriFiltresi.SelectedItem as string;
        SoruListesi.ItemsSource = kategori is null || kategori == Tumu
            ? _tumSorular
            : _tumSorular.Where(s => s.Soru.Kategori == kategori).ToList();
    }

    private void KategoriFiltresi_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!_yukleniyor) Filtrele();
    }

    private void TumunuSec_Click(object? sender, RoutedEventArgs e)
    {
        if (SoruListesi.ItemsSource is not IEnumerable<SecilebilirSoru> gorunenler) return;
        var liste = gorunenler.ToList();
        bool hepsiSecili = liste.All(s => s.Secili);
        foreach (var s in liste) s.Secili = !hepsiSecili;
    }

    private void OzetiGuncelle()
    {
        var secili = _tumSorular.Where(s => s.Secili).ToList();
        int toplamPuan = secili.Sum(s => (int)(s.Puan ?? 0));
        OzetMetni.Text = $"Seçili: {secili.Count} soru  •  Toplam {toplamPuan} puan";
    }

    private async Task OgrencileriYenileAsync()
    {
        try
        {
            var liste = await Servis.Aktif.BagliOgrencileriGetirAsync();
            _bagliSayisi = liste.Count;
            OgrenciListesi.ItemsSource = liste;
            OgrenciBasligi.Text = $"Bağlı Öğrenciler ({liste.Count})";
            SonGuncelleme.Text = $"Son güncelleme: {DateTime.Now:HH:mm:ss}";
        }
        catch
        {
            SonGuncelleme.Text = "Sunucuya ulaşılamıyor, tekrar deneniyor...";
        }
    }

    private async void Gonder_Click(object? sender, RoutedEventArgs e) => await Calistir(async () =>
    {
        var ad = SinavAdiKutusu.Text?.Trim() ?? "";
        var secili = _tumSorular.Where(s => s.Secili).ToList();
        int sure = (int)(SureKutusu.Value ?? 0);

        if (ad == "")          { DurumGoster("Sınav adı girin.", true); return; }
        if (secili.Count == 0) { DurumGoster("En az bir soru seçin.", true); return; }
        if (sure <= 0)         { DurumGoster("Geçerli bir süre girin.", true); return; }
        if (_bagliSayisi == 0) { DurumGoster("Şu an bağlı öğrenci yok.", true); return; }

        var onay = await Diyalog.OnaylaAsync(AnaPencere,
            $"'{ad}' sınavı {secili.Count} soru ve {sure} dakika süreyle " +
            $"{_bagliSayisi} öğrenciye gönderilecek. Onaylıyor musunuz?", "Sınavı Gönder");
        if (!onay) return;

        GonderButonu.IsEnabled = false;
        try
        {
            var sinav = await Servis.Aktif.SinavOlusturAsync(new Sinav
            {
                Ad = ad,
                SureDakika = sure,
                Sorular = secili.Select(s => new SinavSorusu
                {
                    SoruId = s.Soru.Id,
                    Puan = (int)(s.Puan ?? s.Soru.Puan)
                }).ToList()
            });

            await Servis.Aktif.SinavGonderAsync(sinav.Id);
            DurumGoster($"'{ad}' sınavı {DateTime.Now:HH:mm} itibarıyla gönderildi. Süre: {sure} dk.", false);
        }
        finally
        {
            GonderButonu.IsEnabled = true;
        }
    });

    private void DurumGoster(string mesaj, bool hata)
    {
        DurumMetni.Text = mesaj;
        DurumMetni.Foreground = hata ? Brushes.IndianRed : Brushes.SeaGreen;
    }

    private async Task Calistir(Func<Task> islem)
    {
        try { await islem(); }
        catch (Exception ex) { DurumGoster("Hata: " + ex.Message, true); }
    }
}