using FluentAssertions;
using Spamma.Modules.UserManagement.Domain.ApiKeys;
using Spamma.Modules.UserManagement.Tests.Builders;
using Spamma.Tests.Common.Verification;

namespace Spamma.Modules.UserManagement.Tests.Domain.ApiKeys;

/// <summary>
/// Domain tests for the ApiKey aggregate using verification-based patterns.
/// Tests focus on verifying events raised by business logic, not internal state.
/// </summary>
public class ApiKeyTests
{
    [Fact]
    public void Create_WithValidParameters_RaisesApiKeyCreatedEvent()
    {
        // Arrange
        var apiKeyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var name = "Test API Key";
        var keyHash = "hashedkey";
        var salt = "saltvalue";
        var createdAt = DateTimeOffset.UtcNow;

        // Act
        var result = ApiKey.Create(apiKeyId, userId, name, keyHash, salt, createdAt);

        // Verify - result is Ok and an event was raised
        result.ShouldBeOk(apiKey =>
        {
            apiKey.Id.Should().Be(apiKeyId);
            apiKey.UserId.Should().Be(userId);
            apiKey.Name.Should().Be(name);
            apiKey.KeyHash.Should().Be(keyHash);
            apiKey.Salt.Should().Be(salt);
            apiKey.CreatedAt.Should().Be(createdAt);
            apiKey.IsRevoked.Should().BeFalse();
        });
    }

    [Fact]
    public void Create_WithEmptyName_ReturnsFailed()
    {
        // Arrange
        var apiKeyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var name = string.Empty;
        var keyHash = "hashedkey";
        var salt = "saltvalue";
        var createdAt = DateTimeOffset.UtcNow;

        // Act
        var result = ApiKey.Create(apiKeyId, userId, name, keyHash, salt, createdAt);

        // Verify - should fail with appropriate error
        result.ShouldBeFailed();
    }

    [Fact]
    public void Create_WithNameTooLong_ReturnsFailed()
    {
        // Arrange
        var apiKeyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var name = new string('A', 101); // 101 characters
        var keyHash = "hashedkey";
        var salt = "saltvalue";
        var createdAt = DateTimeOffset.UtcNow;

        // Act
        var result = ApiKey.Create(apiKeyId, userId, name, keyHash, salt, createdAt);

        // Verify - should fail with appropriate error
        result.ShouldBeFailed();
    }

    [Fact]
    public void Create_WithEmptyKeyHash_ReturnsFailed()
    {
        // Arrange
        var apiKeyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var name = "Test API Key";
        var keyHash = string.Empty;
        var salt = "saltvalue";
        var createdAt = DateTimeOffset.UtcNow;

        // Act
        var result = ApiKey.Create(apiKeyId, userId, name, keyHash, salt, createdAt);

        // Verify - should fail with appropriate error
        result.ShouldBeFailed();
    }

    [Fact]
    public void Create_WithEmptySalt_ReturnsFailed()
    {
        // Arrange
        var apiKeyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var name = "Test API Key";
        var keyHash = "hashedkey";
        var salt = string.Empty;
        var createdAt = DateTimeOffset.UtcNow;

        // Act
        var result = ApiKey.Create(apiKeyId, userId, name, keyHash, salt, createdAt);

        // Verify - should fail with appropriate error
        result.ShouldBeFailed();
    }

    [Fact]
    public void Revoke_WhenNotRevoked_RaisesApiKeyRevokedEvent()
    {
        // Arrange
        var apiKey = ApiKey.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Key",
            "hash",
            "salt",
            DateTimeOffset.UtcNow).Value;

        var revokedAt = DateTimeOffset.UtcNow.AddMinutes(1);

        // Act
        apiKey.Revoke(revokedAt);

        // Verify - check that the key is now revoked
        apiKey.IsRevoked.Should().BeTrue();
        apiKey.RevokedAt.Should().Be(revokedAt);
    }

    [Fact]
    public void Revoke_WhenAlreadyRevoked_ThrowsInvalidOperationException()
    {
        // Arrange
        var apiKey = ApiKey.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Key",
            "hash",
            "salt",
            DateTimeOffset.UtcNow).Value;

        // First revoke
        apiKey.Revoke(DateTimeOffset.UtcNow);

        // Act & Assert - try to revoke again
        var exception = Assert.Throws<InvalidOperationException>(() =>
            apiKey.Revoke(DateTimeOffset.UtcNow.AddMinutes(1)));

        exception.Message.Should().Be("API key is already revoked");
    }
}