using FluentAssertions;
using Spamma.Modules.UserManagement.Domain.ApiKeys.Events;
using Spamma.Modules.UserManagement.Tests.Builders;
using Spamma.Tests.Common.Verification;

namespace Spamma.Modules.UserManagement.Tests.Domain.ApiKeys;

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

        // Verify event was raised
        apiKey.ShouldHaveRaisedEvent<ApiKeyRevoked>(@event =>
        {
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
    public void IsRevoked_WhenNotRevoked_ShouldReturnFalse()
    {
        // Arrange
        var apiKey = new ApiKeyBuilder()
            .WithId(Guid.NewGuid())
            .WithUserId(Guid.NewGuid())
            .WithName("Test API Key")
            .Build();

        // Act & Assert
        apiKey.IsRevoked.Should().BeFalse();
    }

    [Fact]
    public void IsRevoked_WhenRevoked_ShouldReturnTrue()
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
    }
}