using BluQube.Commands;
using ResultMonad;
using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.UserManagement.Domain.ApiKeys.Events;

namespace Spamma.Modules.UserManagement.Domain.ApiKeys;

public sealed partial class ApiKey : AggregateRoot
{
    private DateTimeOffset? _revokedAt;

    private ApiKey()
    {
    }

    public override Guid Id { get; protected set; }

    public Guid UserId { get; private set; }

    public string Name { get; private set; } = null!;

    public string KeyHashPrefix { get; private set; } = null!;

    public string KeyHash { get; private set; } = null!;

    public DateTimeOffset WhenCreated { get; private set; }

    public DateTimeOffset WhenExpires { get; private set; }

    // Compatibility aliases used by tests and read-model mappings
    public DateTimeOffset CreatedAt => this.WhenCreated;

    public DateTimeOffset ExpiresAt => this.WhenExpires;

    public DateTimeOffset? RevokedAt => this._revokedAt;

    public bool IsRevoked => this._revokedAt.HasValue;

    public bool IsExpired => DateTimeOffset.UtcNow >= this.WhenExpires;

    public bool IsActive => !this.IsRevoked && !this.IsExpired;

    internal static Result<ApiKey, BluQubeErrorData> Create(
        Guid apiKeyId,
        Guid userId,
        string name,
        string keyHashPrefix,
        string keyHash,
        DateTimeOffset whenCreated,
        DateTimeOffset? whenExpires = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Fail<ApiKey, BluQubeErrorData>(new BluQubeErrorData("API_KEY_INVALID_NAME", "API key name cannot be null or empty"));
        }

        if (name.Length > 100)
        {
            return Result.Fail<ApiKey, BluQubeErrorData>(new BluQubeErrorData("API_KEY_INVALID_NAME", "API key name cannot be longer than 100 characters"));
        }

        if (string.IsNullOrWhiteSpace(keyHashPrefix))
        {
            return Result.Fail<ApiKey, BluQubeErrorData>(new BluQubeErrorData("API_KEY_INVALID_PREFIX", "API key prefix hash cannot be null or empty"));
        }

        if (string.IsNullOrWhiteSpace(keyHash))
        {
            return Result.Fail<ApiKey, BluQubeErrorData>(new BluQubeErrorData("API_KEY_INVALID_HASH", "API key hash cannot be null or empty"));
        }

        var apiKey = new ApiKey();
        var expires = whenExpires ?? whenCreated.AddYears(1);
        apiKey.RaiseEvent(new ApiKeyCreated(apiKeyId, userId, name, keyHashPrefix, keyHash, whenCreated, expires));
        return Result.Ok<ApiKey, BluQubeErrorData>(apiKey);
    }

    internal void Revoke(DateTimeOffset revokedAt)
    {
        if (this.IsRevoked)
        {
            throw new InvalidOperationException("API key is already revoked");
        }

        this.RaiseEvent(new ApiKeyRevoked(revokedAt));
    }
}