using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;


namespace OdevSistemi.StudentUI;

/// <param name="Status">HTTP durum kodu. 0 = sunucuya hiç ulaşılamadı.</param>
public record ApiResult(bool Ok, int Status, string? Error = null);

/// <summary>Sunucuyla HTTP üzerinden konuşur: giriş ve cevap gönderme.</summary>
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
            var body = new LoginRequest { Username = username, Password = password };
            var res = await _http.PostAsJsonAsync(BaseUrl + AppConfig.LoginPath, body, Json);
            if (!res.IsSuccessStatusCode)
                return (new ApiResult(false, (int)res.StatusCode), null);

            var data = await res.Content.ReadFromJsonAsync<LoginResponse>(Json);
            Token = data?.Token ?? "";
            return (new ApiResult(true, (int)res.StatusCode), data);
        }
        catch (Exception ex)
        {
            return (new ApiResult(false, 0, ex.Message), null);
        }
    }

    public async Task<ApiResult> SubmitAsync(SubmissionRequest body)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, BaseUrl + AppConfig.SubmitPath)
            {
                Content = JsonContent.Create(body, options: Json)
            };
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
            var res = await _http.SendAsync(req);
            return new ApiResult(res.IsSuccessStatusCode, (int)res.StatusCode);
        }
        catch (Exception ex)
        {
            return new ApiResult(false, 0, ex.Message);
        }
    }
}
