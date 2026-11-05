using Microsoft.EntityFrameworkCore;
using OdevSistemi.Server.Data;
using OdevSistemi.Server.Hubs;
using OdevSistemi.Server.Services;
using OdevSistemi.Shared;

var builder = WebApplication.CreateBuilder(args);

// Sunucu ağdaki tüm bilgisayarlardan erişilebilsin (0.0.0.0 = bütün ağ kartları).
builder.WebHost.UseUrls($"http://0.0.0.0:{ServerInfo.Port}");

builder.Services.AddControllers();
builder.Services.AddSignalR(o =>
{
    o.MaximumReceiveMessageSize = 1024 * 1024;           // uzun klasik cevaplar için 1 MB
    o.EnableDetailedErrors = builder.Environment.IsDevelopment();
    o.ClientTimeoutInterval = TimeSpan.FromSeconds(30);  // 30 sn ses yoksa "koptu" say
    o.KeepAliveInterval = TimeSpan.FromSeconds(10);
});

// 1. kişi: veritabanı (AppDbContext) ve JWT kayıtları buraya eklenecek.
// GEÇİCİ: Kişi 1'in AppDbContext'i gelene kadar (Data/TempModels.cs)
builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite("Data Source=odevsistemi.db"));

// 2. kişi: ağ, dağıtım, puanlama, log servisleri
builder.Services.AddSingleton<ExamSessionManager>();  // bağlı öğrenciler + aktif sınav (bellekte)
builder.Services.AddSingleton<SubmissionLogger>();    // fiziksel .log dosyası
builder.Services.AddScoped<GradingService>();
builder.Services.AddScoped<ExamBroadcaster>();

var app = builder.Build();

// GEÇİCİ: tablo yoksa oluştur (Kişi 1 migration'a geçince kaldırılacak)
using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();

// app.UseAuthentication();   // Kişi 1 JWT ekleyince açılacak
// app.UseAuthorization();

app.MapControllers();
app.MapHub<ExamHub>(ServerInfo.HubPath);

app.Run();
