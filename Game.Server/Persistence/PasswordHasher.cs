using System.Security.Cryptography;

namespace Game.Server.Persistence;

/// <summary>
/// Password hashing with PBKDF2 (SHA-256, 100k iterations). We never store or
/// transmit plaintext passwords; the client sends the password once over the
/// connection and the server immediately hashes it.
/// </summary>
public static class PasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100_000;

    public static (string Hash, string Salt) Hash(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Derive(password, salt);
        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }

    public static bool Verify(string password, string storedHash, string storedSalt)
    {
        byte[] salt = Convert.FromBase64String(storedSalt);
        byte[] expected = Convert.FromBase64String(storedHash);
        byte[] actual = Derive(password, salt);
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }

    private static byte[] Derive(string password, byte[] salt) =>
        Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
}
