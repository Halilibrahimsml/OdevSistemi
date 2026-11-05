using CommunityToolkit.Mvvm.ComponentModel;

namespace OdevSistemi.InstructorUI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty] private string _message = "Eğitmen — proje iskeleti çalışıyor.";
}
