using Avalonia.Controls;
using Avalonia.Interactivity;
using OdevSistemi.InstructorUI.Services;

namespace OdevSistemi.InstructorUI.Views;

public partial class LoginWindow : Window
{
    public LoginWindow() => InitializeComponent();

    private async void GirisYap_Click(object? sender, RoutedEventArgs e)
    {
        var kullaniciAdi = KullaniciAdiKutusu.Text?.Trim() ?? "";
        var sifre = SifreKutusu.Text ?? "";

        if (kullaniciAdi == "" || sifre == "")
        {
            HataGoster("Kullanıcı adı ve şifre boş bırakılamaz.");
            return;
        }

        if (sender is Button b) b.IsEnabled = false;
        try
        {
            if (!await Servis.Aktif.GirisYapAsync(kullaniciAdi, sifre))
            {
                HataGoster("Kullanıcı adı veya şifre hatalı.");
                return;
            }
            new MainWindow(kullaniciAdi).Show();
            Close();
        }
        catch
        {
            HataGoster("Sunucuya bağlanılamadı. Sunucunun çalıştığından emin olun.");
        }
        finally
        {
            if (sender is Button btn) btn.IsEnabled = true;
        }
    }

    private void HataGoster(string mesaj)
    {
        HataMesaji.Text = mesaj;
        HataMesaji.IsVisible = true;
    }
}