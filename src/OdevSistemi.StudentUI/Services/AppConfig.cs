using OdevSistemi.Shared;

namespace OdevSistemi.StudentUI;


/// Öğrenci uygulamasının ayarları.
/// Sunucu adresi, hub yolu ve metot adları OdevSistemi.Shared içinden gelir
/// (ServerInfo, HubEvents, HubMethods), burada tekrar yazılmaz.

public static class AppConfig
{
    ///Sunucu portu (ServerInfo.Port = 5000).
    public const int Port = ServerInfo.Port;

    ///Giriş API'si (1. kişi): POST LoginRequest -> LoginResponse.
    public const string LoginPath = "/api/auth/login";

    
    /// Sayaç, sunucudaki bitiş saatinden bu kadar saniye önce sıfırlanır ve cevaplar otomatik gönderilir.
    /// Sunucu bitişten sonra sadece 2 sn tolerans tanıyor (ExamSessionManager.NetworkToleranceSeconds);
    /// ağ gecikmesi ve küçük saat farkları yüzünden gönderim reddedilmesin diye pay bırakıyoruz.
    
    public const int AutoSubmitSafetySeconds = 3;

    /// Gönderimden sonra öğrenciye puanı gösterilsin mi?
    public const bool ShowScore = true;
}
