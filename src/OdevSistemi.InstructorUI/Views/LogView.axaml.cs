using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using OdevSistemi.InstructorUI.Services;

namespace OdevSistemi.InstructorUI.Views;

public partial class LogView : UserControl
{
    private string _hamIcerik = "";
    private bool _yukleniyor;

    public LogView()
    {
        InitializeComponent();
        _ = Calistir(DosyalariYukleAsync);
    }

    private async Task DosyalariYukleAsync()
    {
        var onceki = DosyaListesi.SelectedItem as string;
        var dosyalar = await Servis.Aktif.LogDosyalariGetirAsync();

        _yukleniyor = true;
        DosyaListesi.ItemsSource = dosyalar;
        DosyaListesi.SelectedItem = onceki is not null && dosyalar.Contains(onceki) ? onceki : dosyalar.FirstOrDefault();
        _yukleniyor = false;

        if (dosyalar.Count == 0)
        {
            _hamIcerik = "";
            LogIcerigi.Text = "Henüz log dosyası yok.";
            DurumMetni.Text = "";
            return;
        }
        await IcerikYukleAsync();
    }

    private async Task IcerikYukleAsync()
    {
        if (DosyaListesi.SelectedItem is not string dosya) return;
        _hamIcerik = await Servis.Aktif.LogIcerigiGetirAsync(dosya);
        Filtrele();
    }

    // Arama kutusuna yazılana göre satırları süzer (Türkçe büyük/küçük harf duyarsız)
    private void Filtrele()
    {
        if (LogIcerigi is null) return;

        var satirlar = _hamIcerik.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        var aranan = AramaKutusu.Text?.Trim() ?? "";
        var gosterilen = aranan == ""
            ? satirlar
            : satirlar.Where(s => s.Contains(aranan, StringComparison.CurrentCultureIgnoreCase)).ToArray();

        LogIcerigi.Text = string.Join(Environment.NewLine, gosterilen);
        DurumMetni.Text = $"{DosyaListesi.SelectedItem}  •  {gosterilen.Length}/{satirlar.Length} satır  " +
                          $"•  Son güncelleme {DateTime.Now:HH:mm:ss}";
    }

    private async void DosyaListesi_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!_yukleniyor) await Calistir(IcerikYukleAsync);
    }

    private void AramaKutusu_TextChanged(object? sender, TextChangedEventArgs e) => Filtrele();

    private async void Yenile_Click(object? sender, RoutedEventArgs e) => await Calistir(DosyalariYukleAsync);

    private async Task Calistir(Func<Task> islem)
    {
        try { await islem(); }
        catch (Exception ex) { DurumMetni.Text = "Hata: " + ex.Message; }
    }
}