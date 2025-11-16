using Spamma.Modules.UserManagement.Domain.ApiKeys;

namespace Spamma.Modules.UserManagement.Tests.Builders;

public class ApiKeyBuilder
{
    private Guid _apiKeyId = Guid.NewGuid();
    private Guid _userId = Guid.NewGuid();
    private string _name = "Test API Key";
    private string _keyHashPrefix = "test_prefix_hash";
    private string _keyHash = BCrypt.Net.BCrypt.HashPassword("sk-testkey123456");
    private DateTimeOffset _createdAt = DateTimeOffset.UtcNow;
    private DateTimeOffset? _expiresAt;
    private DateTimeOffset? _revokedAt;

    public ApiKeyBuilder WithId(Guid apiKeyId)
    {
        this._apiKeyId = apiKeyId;
        return this;
    }

    public ApiKeyBuilder WithUserId(Guid userId)
    {
        this._userId = userId;
        return this;
    }

    public ApiKeyBuilder WithName(string name)
    {
        this._name = name;
        return this;
    }

    public ApiKeyBuilder WithKeyHashPrefix(string keyHashPrefix)
    {
        this._keyHashPrefix = keyHashPrefix;
        return this;
    }

    public ApiKeyBuilder WithKeyHash(string keyHash)
    {
        this._keyHash = keyHash;
        return this;
    }

    public ApiKeyBuilder WithCreatedAt(DateTimeOffset createdAt)
    {
        this._createdAt = createdAt;
        return this;
    }

    public ApiKeyBuilder WithExpiresAt(DateTimeOffset? expiresAt)
    {
        this._expiresAt = expiresAt;
        return this;
    }

    public ApiKeyBuilder RevokedAt(DateTimeOffset? revokedAt)
    {
        this._revokedAt = revokedAt;
        return this;
    }

    public ApiKey Build()
    {
        var result = ApiKey.Create(
            this._apiKeyId,
            this._userId,
            this._name,
            this._keyHashPrefix,
            this._keyHash,
            this._createdAt,
            this._expiresAt);

        var apiKey = result.Value;

        if (this._revokedAt.HasValue)
        {
            apiKey.Revoke(this._revokedAt.Value);
        }

        return apiKey;
    }
}