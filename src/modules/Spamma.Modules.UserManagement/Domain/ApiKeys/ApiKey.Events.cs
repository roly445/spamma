using Spamma.Modules.UserManagement.Domain.ApiKeys.Events;

namespace Spamma.Modules.UserManagement.Domain.ApiKeys;

/// <summary>
/// Event handling for the ApiKey aggregate.
/// </summary>
public partial class ApiKey
{
    internal static ApiKey Replay(ApiKey? aggregate, object @event)
    {
        if (aggregate is null && @event is not ApiKeyCreated)
        {
            throw new ArgumentException("The first event must be ApiKeyCreated.", nameof(@event));
        }

        aggregate ??= new ApiKey();
        aggregate.ApplyEvent(@event);
        return aggregate;
    }

    protected override void ApplyEvent(object @event)
    {
        switch (@event)
        {
            case ApiKeyCreated createdEvent:
                this.ApplyRecorded(createdEvent);
                break;
            case ApiKeyRevoked revokedEvent:
                this.ApplyRecorded(revokedEvent);
                break;
        }
    }

    private void ApplyRecorded(ApiKeyCreated @event)
    {
        this.Id = @event.ApiKeyId;
        this.UserId = @event.UserId;
        this.Name = @event.Name;
        this.KeyHashPrefix = @event.KeyHashPrefix;
        this.KeyHash = @event.KeyHash;
        this.CreatedAt = @event.CreatedAt;
        this.ExpiresAt = @event.ExpiresAt;
    }

    private void ApplyRecorded(ApiKeyRevoked @event)
    {
        this._revokedAt = @event.RevokedAt;
    }
}
