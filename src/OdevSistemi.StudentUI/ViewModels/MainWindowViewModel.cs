using CommunityToolkit.Mvvm.ComponentModel;

namespace OdevSistemi.StudentUI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty] private string _message = "Öğrenci — proje iskeleti çalışıyor.";
}
