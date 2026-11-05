using System.Security.Cryptography;

namespace OdevSistemi.Server.Services;

/// <summary>
/// Şifreler veritabanında asla düz metin tutulmaz.
/// PBKDF2 (SHA-256, 100.000 tur) + her şifre için rastgele "salt" kullanılır.
/// Saklanan biçim: "turSayisi.salt.hash"
/// </summary>
public static class PasswordHasher
{
    private const int Iterations = 100_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public static string Hash(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string stored)
    {
        string[] parts = stored.Split('.');
        if (parts.Length != 3 || !int.TryParse(parts[0], out int iterations))
            return false;

        byte[] salt, expected;
        try
        {
            salt = Convert.FromBase64String(parts[1]);
            expected = Convert.FromBase64String(parts[2]);
        }
        catch (FormatException)
        {
            return false;
        }

        byte[] actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
        // Sabit sürede karşılaştırma: zamanlama saldırılarını engeller.
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}