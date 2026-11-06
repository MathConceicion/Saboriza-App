namespace GastronomiaMarketplace.Infrastructure.Identity;

public static class PasswordHasher
{
    public static string Hash(string senha) => BCrypt.Net.BCrypt.HashPassword(senha, workFactor: 12);
    public static bool Verify(string senha, string hash) => BCrypt.Net.BCrypt.Verify(senha, hash);
}
