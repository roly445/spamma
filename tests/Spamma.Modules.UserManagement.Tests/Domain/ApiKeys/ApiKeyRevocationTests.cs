using FluentAssertions;
using ResultMonad;
using Spamma.Modules.Common.Client;
using Spamma.Modules.UserManagement.Domain.ApiKeys.Events;
using Spamma.Modules.UserManagement.Tests.Builders;
using Spamma.Tests.Common.Verification;

namespace Spamma.Modules.UserManagement.Tests.Domain.ApiKeys;

/// <summary>
/// Unit tests for API key revocation domain logic.
/// </summary>
public class ApiKeyRevocationTests
{
    [Fact]
    public void Revoke_WhenApiKeyIsActive_ShouldRaiseApiKeyRevokedEvent()
    {
        // Arrange
        var apiKey = new ApiKeyBuilder()
            .WithId(Guid.NewGuid())
            .WithUserId(Guid.NewGuid())
            .WithName("Test API Key")
            .Build();
        var revokedAt = DateTimeOffset.UtcNow;

        // Act
        apiKey.Revoke(revokedAt);

        // Assert
        apiKey.IsRevoked.Should().BeTrue();
        apiKey.RevokedAt.Should().Be(revokedAt);

        // Verify event was raised
        apiKey.ShouldHaveRaisedEvent<ApiKeyRevoked>(@event =>
        {
            @event.ApiKeyId.Should().Be(apiKey.Id);
            @event.RevokedAt.Should().Be(revokedAt);
        });
    }

    [Fact]
    public void Revoke_WhenApiKeyIsAlreadyRevoked_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var apiKey = new ApiKeyBuilder()
            .WithId(Guid.NewGuid())
            .WithUserId(Guid.NewGuid())
            .WithName("Test API Key")
            .RevokedAt(DateTimeOffset.UtcNow.AddDays(-1))
            .Build();

        // Act & Assert
        var act = () => apiKey.Revoke(DateTimeOffset.UtcNow);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("API key is already revoked");
    }

    [Fact]
    public void IsRevoked_WhenRevokedAtIsNull_ShouldReturnFalse()
    {
        // Arrange
        var apiKey = new ApiKeyBuilder()
            .WithId(Guid.NewGuid())
            .WithUserId(Guid.NewGuid())
            .WithName("Test API Key")
            .Build();

        // Act & Assert
        apiKey.IsRevoked.Should().BeFalse();
        apiKey.RevokedAt.Should().BeNull();
    }

    [Fact]
    public void IsRevoked_WhenRevokedAtHasValue_ShouldReturnTrue()
    {
        // Arrange
        var revokedAt = DateTimeOffset.UtcNow;
        var apiKey = new ApiKeyBuilder()
            .WithId(Guid.NewGuid())
            .WithUserId(Guid.NewGuid())
            .WithName("Test API Key")
            .RevokedAt(revokedAt)
            .Build();

        // Act & Assert
        apiKey.IsRevoked.Should().BeTrue();
        apiKey.RevokedAt.Should().Be(revokedAt);
    }
}