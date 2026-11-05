using System;
using System.Collections.Generic;

namespace OdevSistemi.InstructorUI.Models;

public enum SoruTuru { CoktanSecmeli, Klasik }

public class Soru
{
    public int Id { get; set; }
    public string Metin { get; set; } = "";
    public SoruTuru Tur { get; set; }
    public string Kategori { get; set; } = "";
    public List<string> Secenekler { get; set; } = new();
    public int DogruSecenekIndex { get; set; } = -1;
    public string? KlasikCevapAnahtari { get; set; }
    public int Puan { get; set; } = 10;

    public string TurAdi => Tur == SoruTuru.CoktanSecmeli ? "Çoktan Seçmeli" : "Klasik";
}

public class SinavSorusu
{
    public int SoruId { get; set; }
    public int Puan { get; set; } = 10;
}

public class Sinav
{
    public int Id { get; set; }
    public string Ad { get; set; } = "";
    public int SureDakika { get; set; }
    public List<SinavSorusu> Sorular { get; set; } = new();
    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;
    public int SoruSayisi { get; set; }
    public string Durum { get; set; } = "";

    public override string ToString() =>
        $"{Ad}  ({OlusturmaTarihi:dd.MM.yyyy HH:mm})" + (Durum == "" ? "" : $"  •  {Durum}");
}

public class BagliOgrenci
{
    public string OgrenciNo { get; set; } = "";
    public string AdSoyad { get; set; } = "";
    public string IpAdresi { get; set; } = "";
    public DateTime BaglanmaZamani { get; set; }
}

public class SinavSonucu
{
    public string OgrenciNo { get; set; } = "";
    public string AdSoyad { get; set; } = "";
    public double Puan { get; set; }
    public double MaxPuan { get; set; }
    public DateTime TeslimZamani { get; set; }
    public string IpAdresi { get; set; } = "";

    public string PuanMetni => $"{Puan:0.#} / {MaxPuan:0.#}";
    public double Yuzde => MaxPuan == 0 ? 0 : Math.Round(100 * Puan / MaxPuan, 1);
}