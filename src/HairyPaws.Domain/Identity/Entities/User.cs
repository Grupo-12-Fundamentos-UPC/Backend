using HairyPaws.Domain.Common.Abstractions;
using HairyPaws.Domain.Identity.Enums;

namespace HairyPaws.Domain.Identity.Entities;

public sealed class User : AuditableEntity
{
    private readonly List<RefreshToken> _refreshTokens = [];

    private User()
    {
    }

    private User(
        string email,
        string passwordHash,
        string firstName,
        string lastName,
        UserRole role,
        DateTimeOffset utcNow,
        string? phoneNumber,
        string? identityDocument,
        string? address,
        string? profileImagePath)
    {
        Id = Guid.NewGuid();
        Email = NormalizeEmail(email);
        PasswordHash = passwordHash;
        Role = role;
        Status = UserStatus.Active;
        VerificationStatus = VerificationStatus.Pending;
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        PhoneNumber = NormalizeOptional(phoneNumber);
        IdentityDocument = NormalizeOptional(identityDocument);
        Address = NormalizeOptional(address);
        ProfileImagePath = NormalizeOptional(profileImagePath);
        CreatedAt = utcNow;
        UpdatedAt = utcNow;
    }

    public string Email { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public UserRole Role { get; private set; }

    public UserStatus Status { get; private set; }

    public VerificationStatus VerificationStatus { get; private set; }

    public string FirstName { get; private set; } = string.Empty;

    public string LastName { get; private set; } = string.Empty;

    public string? PhoneNumber { get; private set; }

    public string? IdentityDocument { get; private set; }

    public string? Address { get; private set; }

    public string? ProfileImagePath { get; private set; }

    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens;

    public bool CanLogin() => DeletedAt is null && Status == UserStatus.Active;

    public static User Create(
        string email,
        string passwordHash,
        string firstName,
        string lastName,
        UserRole role,
        DateTimeOffset utcNow,
        string? phoneNumber = null,
        string? identityDocument = null,
        string? address = null,
        string? profileImagePath = null)
    {
        return new User(
            email,
            passwordHash,
            firstName,
            lastName,
            role,
            utcNow,
            phoneNumber,
            identityDocument,
            address,
            profileImagePath);
    }

    public void ChangePassword(string passwordHash, DateTimeOffset utcNow)
    {
        PasswordHash = passwordHash;
        UpdatedAt = utcNow;
    }

    public void UpdateProfile(
        string firstName,
        string lastName,
        string? phoneNumber,
        string? identityDocument,
        string? address,
        string? profileImagePath,
        DateTimeOffset utcNow)
    {
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        PhoneNumber = NormalizeOptional(phoneNumber);
        IdentityDocument = NormalizeOptional(identityDocument);
        Address = NormalizeOptional(address);
        ProfileImagePath = NormalizeOptional(profileImagePath);
        UpdatedAt = utcNow;
    }

    public void UpdateStatus(UserStatus status, DateTimeOffset utcNow)
    {
        Status = status;
        UpdatedAt = utcNow;
    }

    public void UpdateVerificationStatus(VerificationStatus verificationStatus, DateTimeOffset utcNow)
    {
        VerificationStatus = verificationStatus;
        UpdatedAt = utcNow;
    }

    public RefreshToken AddRefreshToken(string tokenHash, DateTimeOffset expiresAt, DateTimeOffset utcNow)
    {
        var refreshToken = RefreshToken.Create(Id, tokenHash, expiresAt, utcNow);
        _refreshTokens.Add(refreshToken);
        return refreshToken;
    }

    public void RevokeAllActiveRefreshTokens(DateTimeOffset utcNow)
    {
        foreach (var refreshToken in _refreshTokens.Where(static token => token.RevokedAt is null))
        {
            refreshToken.Revoke(utcNow);
        }

        UpdatedAt = utcNow;
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
