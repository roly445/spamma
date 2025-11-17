using FluentAssertions;
using Spamma.Modules.UserManagement.Domain.ApiKeys;
using Spamma.Tests.Common.Verification;

namespace Spamma.Modules.UserManagement.Tests.Domain.ApiKeys;

public class ApiKeyTests
{
    [Fact]
    public void Create_WithValidParameters_RaisesApiKeyCreatedEvent()
    {
        // Arrange
        var apiKeyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var name = "Test API Key";
        var keyHashPrefix = "prefix_hash";
        var keyHash = BCrypt.Net.BCrypt.HashPassword("sk-testkey123456");
        var createdAt = DateTime.UtcNow;

        // Act
        var result = ApiKey.Create(apiKeyId, userId, name, keyHashPrefix, keyHash, createdAt);

        // Verify - result is Ok and an event was raised
        result.ShouldBeOk(apiKey =>
        {
            apiKey.Id.Should().Be(apiKeyId);
            apiKey.UserId.Should().Be(userId);
            apiKey.Name.Should().Be(name);
            apiKey.KeyHashPrefix.Should().Be(keyHashPrefix);
            apiKey.KeyHash.Should().Be(keyHash);
            apiKey.CreatedAt.Should().Be(createdAt);
            apiKey.IsRevoked.Should().BeFalse();
            apiKey.IsActive.Should().BeTrue();
        });
    }

    [Fact]
    public void Create_WithEmptyName_ReturnsFailed()
    {
        // Arrange
        var apiKeyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var name = string.Empty;
        var keyHashPrefix = "prefix_hash";
        var keyHash = "hashedkey";
        var createdAt = DateTime.UtcNow;

        // Act
        var result = ApiKey.Create(apiKeyId, userId, name, keyHashPrefix, keyHash, createdAt);

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
        var keyHashPrefix = "prefix_hash";
        var keyHash = "hashedkey";
        var createdAt = DateTime.UtcNow;

        // Act
        var result = ApiKey.Create(apiKeyId, userId, name, keyHashPrefix, keyHash, createdAt);

        // Verify - should fail with appropriate error
        result.ShouldBeFailed();
    }

    [Fact]
    public void Create_WithEmptyKeyHashPrefix_ReturnsFailed()
    {
        // Arrange
        var apiKeyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var name = "Test API Key";
        var keyHashPrefix = string.Empty;
        var keyHash = "hashedkey";
        var createdAt = DateTime.UtcNow;

        // Act
        var result = ApiKey.Create(apiKeyId, userId, name, keyHashPrefix, keyHash, createdAt);

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
        var keyHashPrefix = "prefix_hash";
        var keyHash = string.Empty;
        var createdAt = DateTime.UtcNow;

        // Act
        var result = ApiKey.Create(apiKeyId, userId, name, keyHashPrefix, keyHash, createdAt);

        // Verify - should fail with appropriate error
        result.ShouldBeFailed();
    }
}
