using System.Collections.Generic;
using System.Threading.Tasks;
using OdevSistemi.InstructorUI.Models;

namespace OdevSistemi.InstructorUI.Services;

public interface IEgitmenServisi
{
    // 1. Kişi: Auth
    Task<bool> GirisYapAsync(string kullaniciAdi, string sifre);

    // 1. Kişi: Soru bankası
    Task<List<string>> KategorileriGetirAsync();
    Task<List<Soru>> SorulariGetirAsync(string? kategori = null);
    Task SoruEkleAsync(Soru soru);
    Task SoruGuncelleAsync(Soru soru);
    Task SoruSilAsync(int id);

    // 1. Kişi: Sınav kaydı
    Task<Sinav> SinavOlusturAsync(Sinav sinav);
    Task<List<Sinav>> SinavlariGetirAsync();

    // 2. Kişi: Ağ, dağıtım, sonuç, log
    Task<List<BagliOgrenci>> BagliOgrencileriGetirAsync();
    Task SinavGonderAsync(int sinavId);
    Task<List<SinavSonucu>> SonuclariGetirAsync(int sinavId);
    Task<List<string>> LogDosyalariGetirAsync();
    Task<string> LogIcerigiGetirAsync(string dosyaAdi);
}

public static class Servis
{
    // Bağlama gününde: new ApiEgitmenServisi("http://localhost:5000")
    public static IEgitmenServisi Aktif { get; set; } = new SahteEgitmenServisi();
}