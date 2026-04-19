using FluentAssertions;
using HairyPaws.Domain.Identity.Entities;

namespace HairyPaws.Tests.Unit.Domain;

public sealed class RefreshTokenTests
{
    [Fact]
    public void CanBeUsed_ShouldReturnFalse_WhenTokenIsExpiredOrRevoked()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", utcNow.AddMinutes(5), utcNow);

        token.CanBeUsed(utcNow).Should().BeTrue();

        token.Revoke(utcNow.AddMinutes(1));
        token.CanBeUsed(utcNow.AddMinutes(2)).Should().BeFalse();

        var expiredToken = RefreshToken.Create(Guid.NewGuid(), "hash-2", utcNow.AddMinutes(-1), utcNow.AddMinutes(-5));
        expiredToken.CanBeUsed(utcNow).Should().BeFalse();
    }
}
