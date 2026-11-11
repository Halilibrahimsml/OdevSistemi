using System.Net.Http.Json;
using System.Text.Json;
using OdevSistemi.Shared;
using OdevSistemi.Shared.Dtos;

namespace OdevSistemi.StudentUI;

///Status: HTTP durum kodu. 0 = sunucuya hiç ulaşılamadı. Error: sunucunun "message" alanı (varsa).
public record ApiResult(bool Ok, int Status, string? Error = null);


/// Sunucuyla HTTP üzerinden konuşur: sadece giriş.
/// Cevap gönderimi HTTP ile değil, SignalR hub üzerinden yapılır (ExamHubService.SubmitAsync).

public class ApiService
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public string BaseUrl { get; private set; } = "";
    public string Token { get; private set; } = "";

    public void SetServer(string ip) => BaseUrl = $"http://{ip}:{AppConfig.Port}";

    public async Task<(ApiResult Result, LoginResponse? Data)> LoginAsync(string username, string password)
    {
        try
        {
            var body = new LoginRequest
            {
                Username = username,
                Password = password,
                RequiredRole = UserRole.Student   // rol tabanlı giriş: eğitmen hesabı bu uygulamaya giremez
            };

            var res = await _http.PostAsJsonAsync(BaseUrl + AppConfig.LoginPath, body, Json);
            if (!res.IsSuccessStatusCode)
                return (new ApiResult(false, (int)res.StatusCode, await ReadMessageAsync(res)), null);

            var data = await res.Content.ReadFromJsonAsync<LoginResponse>(Json);
            Token = data?.Token ?? "";
            return (new ApiResult(true, (int)res.StatusCode), data);
        }
        catch (Exception ex)
        {
            return (new ApiResult(false, 0, ex.Message), null);
        }
    }

    // Sunucu hata durumunda {"message": "..."} döndürüyor (AuthController).
    private static async Task<string?> ReadMessageAsync(HttpResponseMessage res)
    {
        try
        {
            var body = await res.Content.ReadFromJsonAsync<JsonElement>(Json);
            if (body.ValueKind == JsonValueKind.Object && body.TryGetProperty("message", out var m))
                return m.GetString();
        }
        catch { /* gövde JSON değilse mesaj yok */ }
        return null;
    }
}
