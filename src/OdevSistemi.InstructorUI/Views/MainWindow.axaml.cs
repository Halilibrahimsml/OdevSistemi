using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace OdevSistemi.InstructorUI.Views;

public partial class MainWindow : Window
{
    public MainWindow() : this("") { } // tasarımcı için

    public MainWindow(string kullaniciAdi)
    {
        InitializeComponent();
        KullaniciEtiketi.Text = $"Hoş geldiniz, {kullaniciAdi}";
        SayfaAc("soru");
    }

    private void Menu_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.Tag is string sayfa)
            SayfaAc(sayfa);
    }

    private void SayfaAc(string sayfa)
    {
        IcerikAlani.Content = sayfa switch
        {
            "soru"  => new SoruBankasiView(),
            "sinav" => new TextBlock { Text = "Sınav Oluştur (Adım 3)" },
            "sonuc" => new TextBlock { Text = "Sonuçlar (Adım 4)" },
            "log"   => new TextBlock { Text = "Sistem Logları (Adım 4)" },
            _ => null
        };

        foreach (var b in MenuPaneli.Children.OfType<Button>())
            b.Classes.Set("secili", (b.Tag as string) == sayfa);
    }

    private void Cikis_Click(object? sender, RoutedEventArgs e)
    {
        new LoginWindow().Show();
        Close();
    }
}