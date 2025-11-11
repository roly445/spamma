using BluQube.Commands;
using ResultMonad;
using Spamma.Modules.Common.Client;
using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.UserManagement.Domain.ApiKeys.Events;

namespace Spamma.Modules.UserManagement.Domain.ApiKeys;

public class ApiKey : AggregateRoot
{
    private string _name = string.Empty;
    private string _keyHash = string.Empty;
    private string _salt = string.Empty;
    private DateTimeOffset _createdAt;
    private DateTimeOffset? _revokedAt;
    private DateTimeOffset? _expiresAt;

    private ApiKey()
    {
    }

    public override Guid Id { get; protected set; }

    public Guid UserId { get; private set; }

    public string Name => this._name;

    public string KeyHash => this._keyHash;

    public string Salt => this._salt;

    public DateTimeOffset CreatedAt => this._createdAt;

    public DateTimeOffset? RevokedAt => this._revokedAt;

    public DateTimeOffset? ExpiresAt => this._expiresAt;

    public bool IsRevoked => this._revokedAt.HasValue;

    public bool IsExpired => this._expiresAt.HasValue && this._expiresAt.Value <= DateTimeOffset.UtcNow;

    public static Result<ApiKey, BluQubeErrorData> Create(Guid apiKeyId, Guid userId, string name, string keyHash, string salt, DateTimeOffset createdAt, DateTimeOffset? expiresAt = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Fail<ApiKey, BluQubeErrorData>(new BluQubeErrorData("API_KEY_INVALID_NAME", "API key name cannot be null or empty"));
        }

        if (name.Length > 100)
        {
            return Result.Fail<ApiKey, BluQubeErrorData>(new BluQubeErrorData("API_KEY_INVALID_NAME", "API key name cannot be longer than 100 characters"));
        }

        if (string.IsNullOrWhiteSpace(keyHash))
        {
            return Result.Fail<ApiKey, BluQubeErrorData>(new BluQubeErrorData("API_KEY_INVALID_HASH", "API key hash cannot be null or empty"));
        }

        if (string.IsNullOrWhiteSpace(salt))
        {
            return Result.Fail<ApiKey, BluQubeErrorData>(new BluQubeErrorData("API_KEY_INVALID_SALT", "API key salt cannot be null or empty"));
        }

        if (expiresAt.HasValue && expiresAt.Value <= createdAt)
        {
            return Result.Fail<ApiKey, BluQubeErrorData>(new BluQubeErrorData("API_KEY_INVALID_EXPIRATION", "API key expiration must be after creation time"));
        }

        var apiKey = new ApiKey();
        apiKey.RaiseEvent(new ApiKeyCreated(apiKeyId, userId, name, keyHash, salt, createdAt, expiresAt));
        return Result.Ok<ApiKey, BluQubeErrorData>(apiKey);
    }

    public void Revoke(DateTimeOffset revokedAt)
    {
        if (this.IsRevoked)
        {
            throw new InvalidOperationException("API key is already revoked");
        }

        this.RaiseEvent(new ApiKeyRevoked(this.Id, revokedAt));
    }

    public void Expire(DateTimeOffset expiredAt)
    {
        if (this.IsExpired)
        {
            throw new InvalidOperationException("API key is already expired");
        }

        if (this.IsRevoked)
        {
            throw new InvalidOperationException("Cannot expire a revoked API key");
        }

        this.RaiseEvent(new ApiKeyExpired(this.Id, expiredAt));
    }

    public void Renew(string newKeyHash, string newSalt, DateTimeOffset renewedAt, DateTimeOffset newExpiresAt)
    {
        if (this.IsRevoked)
        {
            throw new InvalidOperationException("Cannot renew a revoked API key");
        }

        if (newExpiresAt <= renewedAt)
        {
            throw new InvalidOperationException("New expiration must be after renewal time");
        }

        this.RaiseEvent(new ApiKeyRenewed(this.Id, newKeyHash, newSalt, renewedAt, newExpiresAt));
    }

    protected override void ApplyEvent(object @event)
    {
        switch (@event)
        {
            case ApiKeyCreated createdEvent:
                this.Apply(createdEvent);
                break;
            case ApiKeyRevoked revokedEvent:
                this.Apply(revokedEvent);
                break;
            case ApiKeyExpired expiredEvent:
                this.Apply(expiredEvent);
                break;
            case ApiKeyRenewed renewedEvent:
                this.Apply(renewedEvent);
                break;
        }
    }

    private void Apply(ApiKeyCreated @event)
    {
        this.Id = @event.ApiKeyId;
        this.UserId = @event.UserId;
        this._name = @event.Name;
        this._keyHash = @event.KeyHash;
        this._salt = @event.Salt;
        this._createdAt = @event.CreatedAt;
        this._expiresAt = @event.ExpiresAt;
    }

    private void Apply(ApiKeyRevoked @event)
    {
        this._revokedAt = @event.RevokedAt;
    }

    private void Apply(ApiKeyExpired @event)
    {
        this._expiresAt = @event.ExpiredAt;
    }

    private void Apply(ApiKeyRenewed @event)
    {
        this._keyHash = @event.NewKeyHash;
        this._salt = @event.NewSalt;
        this._expiresAt = @event.NewExpiresAt;

        // Clear revocation if it was set (renewal reactivates the key)
        this._revokedAt = null;
    }
}