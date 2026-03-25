using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using FitnessApp.Data;
using FitnessApp.Models;

namespace FitnessApp.Services;

public class AuthService
{
    private readonly AppDbContext _db;

    public AuthService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<(bool Success, string Error)> RegisterAsync(string fullName, string username, string email, string password)
    {
        var normalizedEmail = email.ToLowerInvariant().Trim();
        var normalizedUsername = username.Trim();

        if (await _db.Users.AnyAsync(u => u.Email == normalizedEmail))
            return (false, "An account with this email already exists.");

        if (await _db.Users.AnyAsync(u => u.Username == normalizedUsername))
            return (false, "This username is already taken.");

        var user = new User
        {
            FullName = fullName.Trim(),
            Username = normalizedUsername,
            Email = normalizedEmail,
            PasswordHash = HashPassword(password),
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<User?> LoginAsync(string email, string password)
    {
        var normalizedEmail = email.ToLowerInvariant().Trim();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        if (user is null || !VerifyPassword(password, user.PasswordHash))
            return null;

        return user;
    }

    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        var parts = storedHash.Split('.');
        if (parts.Length != 2) return false;

        var salt = Convert.FromBase64String(parts[0]);
        var hash = Convert.FromBase64String(parts[1]);
        var computedHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(hash, computedHash);
    }
}
