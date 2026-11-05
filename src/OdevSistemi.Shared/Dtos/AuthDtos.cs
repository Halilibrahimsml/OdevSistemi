namespace OdevSistemi.Shared.Dtos;

/// <summary>Giriş isteği. Arayüzler sunucuya bunu gönderir.</summary>
public class LoginRequest
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";

    /// <summary>
    /// Hangi uygulamadan giriş yapılıyor? Eğitmen uygulaması Instructor, öğrenci uygulaması Student gönderir.
    /// Böylece öğrenci, eğitmen uygulamasına giremez (rol tabanlı giriş).
    /// </summary>
    public UserRole? RequiredRole { get; set; }
}

/// <summary>Başarılı girişte sunucunun döndüğü bilgi.</summary>
public class LoginResponse
{
    public string Token { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
    public UserDto User { get; set; } = new();
}

/// <summary>Kullanıcı bilgisi (şifre hariç).</summary>
public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string FullName { get; set; } = "";
    public UserRole Role { get; set; }
    public string ClassName { get; set; } = "";
}