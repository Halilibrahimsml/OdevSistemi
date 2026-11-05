using OdevSistemi.Server.Hubs;
using OdevSistemi.Shared;

var builder = WebApplication.CreateBuilder(args);

// Sunucu ağdaki tüm bilgisayarlardan erişilebilsin (0.0.0.0 = bütün ağ kartları).
builder.WebHost.UseUrls($"http://0.0.0.0:{ServerInfo.Port}");

builder.Services.AddControllers();
builder.Services.AddSignalR();

// 1. kişi: veritabanı (AppDbContext) ve JWT kayıtları buraya eklenecek.

var app = builder.Build();

app.MapControllers();
app.MapHub<ExamHub>(ServerInfo.HubPath);

app.Run();
