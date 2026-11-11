using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OdevSistemi.InstructorUI.Models;

// PDF'ten okunan, henüz soru bankasına eklenmemiş bir soru
public partial class IceAktarilanSoru : ObservableObject
{
    public int No { get; }
    public bool Duzenlendi { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Metin))]
    [NotifyPropertyChangedFor(nameof(TurAdi))]
    [NotifyPropertyChangedFor(nameof(CevapMetni))]
    private Soru _soru;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Gecerli))]
    [NotifyPropertyChangedFor(nameof(Durum))]
    private string? _hata;

    [ObservableProperty] private bool _secili;

    public IceAktarilanSoru(int no, Soru soru)
    {
        No = no;
        _soru = soru;
    }

    public string Metin => Soru.Metin;
    public string TurAdi => Soru.TurAdi;
    public bool Gecerli => Hata is null;
    public string Durum => Hata is null ? "Hazır" : "Düzeltilmeli: " + Hata;

    public string CevapMetni
    {
        get
        {
            if (Soru.Tur == SoruTuru.Klasik) return Soru.KlasikCevapAnahtari ?? "-";
            int i = Soru.DogruSecenekIndex;
            return i >= 0 && i < Soru.Secenekler.Count ? $"{"ABCDEF"[i]}) {Soru.Secenekler[i]}" : "-";
        }
    }
}

public class PdfOkumaSonucu
{
    public List<IceAktarilanSoru> Sorular { get; } = new();
    public List<string> Uyarilar { get; } = new();
}