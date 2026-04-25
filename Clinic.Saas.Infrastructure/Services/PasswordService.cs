using Clinic.Saas.Service.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Clinic.Saas.Infrastructure.Services;

public class PasswordService : IPasswordService
{
    private readonly PasswordHasher<object> _passwordHasher = new();
    private static readonly object UserKey = new();

    public string HashPassword(string password)
    {
        return _passwordHasher.HashPassword(UserKey, password);
    }

    public bool VerifyPassword(string hashedPassword, string providedPassword)
    {
        var result = _passwordHasher.VerifyHashedPassword(UserKey, hashedPassword, providedPassword);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
