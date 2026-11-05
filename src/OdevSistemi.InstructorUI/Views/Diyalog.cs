using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace OdevSistemi.InstructorUI.Views;

public static class Diyalog
{
    public static async Task<bool> OnaylaAsync(Window? sahip, string mesaj, string baslik = "Onay")
    {
        if (sahip is null) return false;

        var pencere = new Window
        {
            Title = baslik, Width = 400, SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner, CanResize = false
        };
        var evet = new Button { Content = "Evet", MinWidth = 80, HorizontalContentAlignment = HorizontalAlignment.Center };
        var hayir = new Button { Content = "Vazgeç", MinWidth = 80, HorizontalContentAlignment = HorizontalAlignment.Center };
        evet.Click += (_, _) => pencere.Close(true);
        hayir.Click += (_, _) => pencere.Close(false);

        pencere.Content = new StackPanel
        {
            Margin = new Thickness(20), Spacing = 16,
            Children =
            {
                new TextBlock { Text = mesaj, TextWrapping = TextWrapping.Wrap },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 8, Children = { hayir, evet }
                }
            }
        };
        return await pencere.ShowDialog<bool>(sahip);
    }
}