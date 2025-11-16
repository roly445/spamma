using Spamma.Modules.UserManagement.Domain.ApiKeys.Events;

namespace Spamma.Modules.UserManagement.Domain.ApiKeys;

public partial class ApiKey
{
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
        this.Name = @event.Name;
        this.KeyHashPrefix = @event.KeyHashPrefix;
        this.KeyHash = @event.KeyHash;
        this.WhenCreated = @event.WhenCreated;
        this.WhenExpires = @event.WhenExpires;
    }

    private void Apply(ApiKeyRevoked @event)
    {
        this._revokedAt = @event.RevokedAt;
    }
}