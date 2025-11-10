using Spamma.Modules.UserManagement.Domain.ApiKeys;

namespace Spamma.Modules.UserManagement.Tests.Builders;

public class ApiKeyBuilder
{
    private Guid _apiKeyId = Guid.NewGuid();
    private Guid _userId = Guid.NewGuid();
    private string _name = "Test API Key";
    private string _keyHash = "testkeyhash";
    private string _salt = "testsalt";
    private DateTimeOffset _createdAt = DateTimeOffset.UtcNow;
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

    public ApiKeyBuilder WithKeyHash(string keyHash)
    {
        this._keyHash = keyHash;
        return this;
    }

    public ApiKeyBuilder WithSalt(string salt)
    {
        this._salt = salt;
        return this;
    }

    public ApiKeyBuilder WithCreatedAt(DateTimeOffset createdAt)
    {
        this._createdAt = createdAt;
        return this;
    }

    public ApiKeyBuilder RevokedAt(DateTimeOffset? revokedAt)
    {
        this._revokedAt = revokedAt;
        return this;
    }

    public ApiKey Build()
    {
        var result = ApiKey.Create(this._apiKeyId, this._userId, this._name, this._keyHash, this._salt, this._createdAt);
        var apiKey = result.Value;

        if (this._revokedAt.HasValue)
        {
            apiKey.Revoke(this._revokedAt.Value);
        }

        return apiKey;
    }
}