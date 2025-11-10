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

    public bool IsRevoked => this._revokedAt.HasValue;

    public static ApiKey Create(Guid apiKeyId, Guid userId, string name, string keyHash, string salt, DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be null or empty", nameof(name));
        }

        if (name.Length > 100)
        {
            throw new ArgumentException("Name cannot be longer than 100 characters", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(keyHash))
        {
            throw new ArgumentException("KeyHash cannot be null or empty", nameof(keyHash));
        }

        if (string.IsNullOrWhiteSpace(salt))
        {
            throw new ArgumentException("Salt cannot be null or empty", nameof(salt));
        }

        var apiKey = new ApiKey();
        apiKey.RaiseEvent(new ApiKeyCreated(apiKeyId, userId, name, keyHash, salt, createdAt));
        return apiKey;
    }

    public void Revoke(DateTimeOffset revokedAt)
    {
        if (this.IsRevoked)
        {
            throw new InvalidOperationException("API key is already revoked");
        }

        this.RaiseEvent(new ApiKeyRevoked(this.Id, revokedAt));
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
    }

    private void Apply(ApiKeyRevoked @event)
    {
        this._revokedAt = @event.RevokedAt;
    }
}