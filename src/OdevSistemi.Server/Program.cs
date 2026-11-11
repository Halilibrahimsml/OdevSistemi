using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OdevSistemi.Server.Data;
using OdevSistemi.Server.Hubs;
using OdevSistemi.Server.Services;
using OdevSistemi.Shared;

var builder = WebApplication.CreateBuilder(args);

// Sunucu ağdaki tüm bilgisayarlardan erişilebilsin (0.0.0.0 = bütün ağ kartları).
builder.WebHost.UseUrls($"http://0.0.0.0:{ServerInfo.Port}");

// ---- Veritabanı (SQLite, tek dosya) — 1. kişi ----
string dbPath = Path.Combine(builder.Environment.ContentRootPath, "odevsistemi.db");
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite($"Data Source={dbPath}"));

// ---- Giriş / JWT — 1. kişi ----
JwtSettings jwt = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
                  ?? throw new InvalidOperationException("appsettings.json içinde 'Jwt' ayarı bulunamadı.");
builder.Services.AddSingleton(jwt);
builder.Services.AddSingleton<TokenService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };

        // SignalR bağlantısında token, adresin sonunda "?access_token=..." olarak gelir.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                string? accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) &&
                    context.HttpContext.Request.Path.StartsWithSegments(ServerInfo.HubPath))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddSignalR(o =>
{
    o.MaximumReceiveMessageSize = 1024 * 1024;           // uzun klasik cevaplar için 1 MB
    o.EnableDetailedErrors = builder.Environment.IsDevelopment();
    o.ClientTimeoutInterval = TimeSpan.FromSeconds(30);  // 30 sn ses yoksa "koptu" say
    o.KeepAliveInterval = TimeSpan.FromSeconds(10);
});

// ---- Ağ, dağıtım, puanlama, log servisleri 
builder.Services.AddSingleton<ExamSessionManager>();  // bağlı öğrenciler + aktif sınav (bellekte)
builder.Services.AddSingleton<SubmissionLogger>();    // fiziksel .log dosyası
builder.Services.AddScoped<GradingService>();
builder.Services.AddScoped<ExamBroadcaster>();

var app = builder.Build();

// Sunucu açılırken: veritabanı yoksa oluştur, boşsa başlangıç hesaplarını ekle.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    DbSeeder.Seed(db);
}
Console.WriteLine($"Veritabanı: {dbPath}");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ExamHub>(ServerInfo.HubPath);

app.Run();