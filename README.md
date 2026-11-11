# Ödev ve Sınav Sistemi

Yazılım Geliştirme I — 2026-2027 Güz dönemi projesi. C# (.NET 10), ASP.NET Core, SignalR, Avalonia.
1.kişi Halil İbrahim Şamlı-230229035
2.kişi Arda Kundak-230229060
3.kişi Şevval Bayrak-260229090
4.kişi Berivan Yıldırım-240229065
## Projeler

| Klasör | Ne | Sorumlu |
|---|---|---|
| `src/OdevSistemi.Shared` | Ortak DTO sınıfları (herkes kullanır) | 1. ve 2. kişi |
| `src/OdevSistemi.Server` | Web API + veritabanı + SignalR | 1. ve 2. kişi |
| `src/OdevSistemi.InstructorUI` | Eğitmen masaüstü uygulaması | 3. kişi |
| `src/OdevSistemi.StudentUI` | Öğrenci masaüstü uygulaması | 4. kişi |

## Çalıştırma

```
dotnet build
dotnet run --project src/OdevSistemi.Server
dotnet run --project src/OdevSistemi.InstructorUI
dotnet run --project src/OdevSistemi.StudentUI
```
Gereken program: .NET 10 SDK

Hazır hesaplar: admin / admin123 ve öğrenci numaraları / 1234

Farklı bilgisayardan bağlanmak için: sunucu eğitmen bilgisayarında çalışır, öğrenci ipconfig ile bulunan IP’yi girer

Sunucu testi: tarayıcıda http://localhost:5000/api/health

## Çalışma kuralları

- `main`'e doğrudan push yok. Herkes kendi branch'inde çalışır, Pull Request ile birleştirir.
- Branch adları: `kisi1-backend`, `kisi2-signalr`, `kisi3-egitmen-ui`, `kisi4-ogrenci-ui`
- İşe başlamadan önce: `git pull origin main`
- `Program.cs` değiştirmeden önce gruba haber verin.
