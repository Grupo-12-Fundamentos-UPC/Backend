using HairyPaws.Domain.Identity.Entities;

namespace HairyPaws.Application.Common.Interfaces;

public interface IPasswordHasher
{
    string HashPassword(User user, string password);

    bool VerifyHashedPassword(User user, string passwordHash, string providedPassword);
}
