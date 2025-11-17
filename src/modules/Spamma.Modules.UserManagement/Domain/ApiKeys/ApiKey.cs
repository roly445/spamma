using BluQube.Commands;
using ResultMonad;
using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.UserManagement.Client.Contracts;
using Spamma.Modules.UserManagement.Domain.ApiKeys.Events;

namespace Spamma.Modules.UserManagement.Domain.ApiKeys;

/// <summary>
/// Business logic for API keys.
/// </summary>
public sealed partial class ApiKey : AggregateRoot
{
    private DateTime? _revokedAt;

    private ApiKey()
    {
    }

    public override Guid Id { get; protected set; }

    internal Guid UserId { get; private set; }

    internal string Name { get; private set; } = null!;

    internal string KeyHashPrefix { get; private set; } = null!;

    internal string KeyHash { get; private set; } = null!;

    internal DateTime CreatedAt { get; private set; }

    internal DateTimeOffset ExpiresAt { get; private set; }

    internal DateTime RevokedAt => this._revokedAt ?? throw new InvalidOperationException("API key is not revoked. Check IsRevoked before accessing RevokedAt.");

    internal bool IsRevoked => this._revokedAt.HasValue;

    internal bool IsExpired => DateTimeOffset.UtcNow >= this.ExpiresAt;

    internal bool IsActive => !this.IsRevoked && !this.IsExpired;

    internal static Result<ApiKey, BluQubeErrorData> Create(
        Guid apiKeyId,
        Guid userId,
        string name,
        string keyHashPrefix,
        string keyHash,
        DateTime createdAt,
        DateTime? expiresAt = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Fail<ApiKey, BluQubeErrorData>(new BluQubeErrorData(UserManagementErrorCodes.ApiKeyInvalidName, "API key name cannot be null or empty"));
        }

        if (name.Length > 100)
        {
            return Result.Fail<ApiKey, BluQubeErrorData>(new BluQubeErrorData(UserManagementErrorCodes.ApiKeyInvalidName, "API key name cannot be longer than 100 characters"));
        }

        if (string.IsNullOrWhiteSpace(keyHashPrefix))
        {
            return Result.Fail<ApiKey, BluQubeErrorData>(new BluQubeErrorData(UserManagementErrorCodes.ApiKeyInvalidPrefix, "API key prefix hash cannot be null or empty"));
        }

        if (string.IsNullOrWhiteSpace(keyHash))
        {
            return Result.Fail<ApiKey, BluQubeErrorData>(new BluQubeErrorData(UserManagementErrorCodes.ApiKeyInvalidHash, "API key hash cannot be null or empty"));
        }

        var apiKey = new ApiKey();
        var expires = expiresAt ?? createdAt.AddYears(1);
        apiKey.RaiseEvent(new ApiKeyCreated(apiKeyId, userId, name, keyHashPrefix, keyHash, createdAt, expires));
        return Result.Ok<ApiKey, BluQubeErrorData>(apiKey);
    }

    internal void Revoke(DateTime revokedAt)
    {
        if (this.IsRevoked)
        {
            throw new InvalidOperationException("API key is already revoked");
        }

        this.RaiseEvent(new ApiKeyRevoked(revokedAt));
    }
}