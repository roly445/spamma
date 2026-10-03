using Spamma.Modules.UserManagement.Domain.PasskeyAggregate.Events;

namespace Spamma.Modules.UserManagement.Domain.PasskeyAggregate;

/// <summary>
/// Event handling for the Passkey aggregate.
/// </summary>
public partial class Passkey
{
    internal static Passkey Replay(Passkey? aggregate, object @event)
    {
        if (aggregate is null && @event is not PasskeyRegistered)
        {
            throw new ArgumentException("The first event must be PasskeyRegistered.", nameof(@event));
        }

        aggregate ??= new Passkey();
        aggregate.ApplyEvent(@event);
        return aggregate;
    }

    protected override void ApplyEvent(object @event)
    {
        switch (@event)
        {
            case PasskeyRegistered registeredEvent:
                this.ApplyRecorded(registeredEvent);
                break;
            case PasskeyAuthenticated authenticatedEvent:
                this.ApplyRecorded(authenticatedEvent);
                break;
            case PasskeyRevoked revokedEvent:
                this.ApplyRecorded(revokedEvent);
                break;
        }
    }

    private void ApplyRecorded(PasskeyRegistered @event)
    {
        this.Id = @event.PasskeyId;
        this.UserId = @event.UserId;
        this.CredentialId = @event.CredentialId;
        this.PublicKey = @event.PublicKey;
        this.SignCount = @event.SignCount;
        this.DisplayName = @event.DisplayName;
        this.Algorithm = @event.Algorithm;
        this.RegisteredAt = @event.RegisteredAt;
    }

    private void ApplyRecorded(PasskeyAuthenticated @event)
    {
        this.SignCount = @event.NewSignCount;
        this._lastUsedAt = @event.UsedAt;
    }

    private void ApplyRecorded(PasskeyRevoked @event)
    {
        this._revokedAt = @event.RevokedAt;
        this._revokedByUserId = @event.RevokedByUserId;
    }
}
