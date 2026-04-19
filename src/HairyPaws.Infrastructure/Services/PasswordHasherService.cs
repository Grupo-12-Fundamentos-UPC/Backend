using HairyPaws.Application.Common.Interfaces;
using HairyPaws.Domain.Identity.Entities;
using Microsoft.AspNetCore.Identity;

namespace HairyPaws.Infrastructure.Services;

public sealed class PasswordHasherService : IPasswordHasher
{
    private readonly PasswordHasher<User> _passwordHasher = new();

    public string HashPassword(User user, string password) => _passwordHasher.HashPassword(user, password);

    public bool VerifyHashedPassword(User user, string passwordHash, string providedPassword)
    {
        return _passwordHasher.VerifyHashedPassword(user, passwordHash, providedPassword)
            is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
