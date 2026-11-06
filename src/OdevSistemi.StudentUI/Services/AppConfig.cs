using OdevSistemi.Shared;

namespace OdevSistemi.StudentUI;

/// <summary>
/// Öğrenci uygulamasının ayarları.
/// Sunucu adresi, hub yolu ve metot adları OdevSistemi.Shared içinden gelir
/// (ServerInfo, HubEvents, HubMethods), burada tekrar yazılmaz.
/// </summary>
public static class AppConfig
{
    /// <summary>Sunucu portu (ServerInfo.Port = 5000).</summary>
    public const int Port = ServerInfo.Port;

    /// <summary>Giriş API'si (1. kişi): POST LoginRequest -> LoginResponse.</summary>
    public const string LoginPath = "/api/auth/login";

    /// <summary>
    /// Sayaç, sunucudaki bitiş saatinden bu kadar saniye önce sıfırlanır ve cevaplar otomatik gönderilir.
    /// Sunucu bitişten sonra sadece 2 sn tolerans tanıyor (ExamSessionManager.NetworkToleranceSeconds);
    /// ağ gecikmesi ve küçük saat farkları yüzünden gönderim reddedilmesin diye pay bırakıyoruz.
    /// </summary>
    public const int AutoSubmitSafetySeconds = 3;

    /// <summary>Gönderimden sonra öğrenciye puanı gösterilsin mi? (Grupça karar verilecek.)</summary>
    public const bool ShowScore = true;
}
