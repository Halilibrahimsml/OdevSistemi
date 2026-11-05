using OdevSistemi.Server.Entities;
using OdevSistemi.Server.Services;
using OdevSistemi.Shared;

namespace OdevSistemi.Server.Data;

/// <summary>Veritabanı boşsa başlangıç hesaplarını ekler.</summary>
public static class DbSeeder
{
    public static void Seed(AppDbContext db)
    {
        if (db.Users.Any())
            return; // daha önce eklenmiş

        db.Users.AddRange(
            new AppUser
            {
                Username = "admin",
                PasswordHash = PasswordHasher.Hash("admin123"),
                FullName = "Eğitmen (Yönetici)",
                Role = UserRole.Instructor
            },
            new AppUser
            {
                Username = "2024001",
                PasswordHash = PasswordHasher.Hash("1234"),
                FullName = "Ali Yılmaz",
                Role = UserRole.Student,
                ClassName = "1-A"
            },
            new AppUser
            {
                Username = "2024002",
                PasswordHash = PasswordHasher.Hash("1234"),
                FullName = "Ayşe Demir",
                Role = UserRole.Student,
                ClassName = "1-A"
            },
            new AppUser
            {
                Username = "2024003",
                PasswordHash = PasswordHasher.Hash("1234"),
                FullName = "Mehmet Kaya",
                Role = UserRole.Student,
                ClassName = "1-B"
            });

        db.SaveChanges();
    }
}