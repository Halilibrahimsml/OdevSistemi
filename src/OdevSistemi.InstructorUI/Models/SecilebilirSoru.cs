using CommunityToolkit.Mvvm.ComponentModel;

namespace OdevSistemi.InstructorUI.Models;

public partial class SecilebilirSoru : ObservableObject
{
    public Soru Soru { get; }

    [ObservableProperty] private bool _secili;
    [ObservableProperty] private decimal? _puan;

    public SecilebilirSoru(Soru soru)
    {
        Soru = soru;
        _puan = soru.Puan;
    }
}