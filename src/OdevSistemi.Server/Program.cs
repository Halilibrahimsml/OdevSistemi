using Microsoft.EntityFrameworkCore;
using OdevSistemi.Server.Data;
using OdevSistemi.Server.Hubs;
using OdevSistemi.Shared;

var builder = WebApplication.CreateBuilder(args);

// Sunucu ağdaki tüm bilgisayarlardan erişilebilsin (0.0.0.0 = bütün ağ kartları).
builder.WebHost.UseUrls($"http://0.0.0.0:{ServerInfo.Port}");

// ---- Veritabanı (SQLite, tek dosya) ----
string dbPath = Path.Combine(builder.Environment.ContentRootPath, "odevsistemi.db");
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddControllers();
builder.Services.AddSignalR();

var app = builder.Build();

// Sunucu açılırken veritabanı dosyası ve tablolar yoksa oluştur.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}
Console.WriteLine($"Veritabanı: {dbPath}");

app.MapControllers();
app.MapHub<ExamHub>(ServerInfo.HubPath);

app.Run();