using Spamma.Modules.UserManagement.Domain.PasskeyAggregate.Events;

namespace Spamma.Modules.UserManagement.Domain.PasskeyAggregate;

/// <summary>
/// Event handling for the Passkey aggregate root.
/// </summary>
public partial class Passkey
{
    protected override void ApplyEvent(object @event)
    {
        switch (@event)
        {
            case PasskeyRegistered registeredEvent:
                this.Apply(registeredEvent);
                break;
            case PasskeyAuthenticated authenticatedEvent:
                this.Apply(authenticatedEvent);
                break;
            case PasskeyRevoked revokedEvent:
                this.Apply(revokedEvent);
                break;
        }
    }

    private void Apply(PasskeyRegistered @event)
    {
        this.UserId = @event.UserId;
        this.CredentialId = @event.CredentialId;
        this.PublicKey = @event.PublicKey;
        this.SignCount = @event.SignCount;
        this.DisplayName = @event.DisplayName;
        this.Algorithm = @event.Algorithm;
        this.RegisteredAt = @event.RegisteredAt;
        this.IsRevoked = false;
        this.LastUsedAt = null;
        this.RevokedAt = null;
        this.RevokedByUserId = null;
    }

    private void Apply(PasskeyAuthenticated @event)
    {
        this.SignCount = @event.NewSignCount;
        this.LastUsedAt = @event.UsedAt;
    }

    private void Apply(PasskeyRevoked @event)
    {
        this.IsRevoked = true;
        this.RevokedAt = @event.RevokedAt;
        this.RevokedByUserId = @event.RevokedByUserId;
    }
}