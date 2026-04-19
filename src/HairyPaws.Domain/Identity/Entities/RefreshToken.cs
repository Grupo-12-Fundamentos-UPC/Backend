using HairyPaws.Domain.Common.Abstractions;

namespace HairyPaws.Domain.Identity.Entities;

public sealed class RefreshToken : Entity
{
    private RefreshToken()
    {
    }

    private RefreshToken(Guid userId, string tokenHash, DateTimeOffset expiresAt, DateTimeOffset createdAt)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        CreatedAt = createdAt;
    }

    public Guid UserId { get; private set; }

    public User User { get; private set; } = null!;

    public string TokenHash { get; private set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public bool IsExpired(DateTimeOffset utcNow) => ExpiresAt <= utcNow;

    public bool IsRevoked() => RevokedAt is not null;

    public bool CanBeUsed(DateTimeOffset utcNow) => !IsExpired(utcNow) && !IsRevoked();

    public static RefreshToken Create(Guid userId, string tokenHash, DateTimeOffset expiresAt, DateTimeOffset createdAt)
    {
        return new RefreshToken(userId, tokenHash, expiresAt, createdAt);
    }

    public void Revoke(DateTimeOffset revokedAt)
    {
        if (RevokedAt is not null)
        {
            return;
        }

        RevokedAt = revokedAt;
    }
}
