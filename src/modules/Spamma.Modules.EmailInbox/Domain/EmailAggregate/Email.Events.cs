﻿using Spamma.Modules.EmailInbox.Domain.EmailAggregate.Events;

namespace Spamma.Modules.EmailInbox.Domain.EmailAggregate;

public partial class Email
{
    protected override void ApplyEvent(object @event)
    {
        switch (@event)
        {
            case EmailReceived emailReceived:
                this.Apply(emailReceived);
                break;
            case EmailDeleted emailDeleted:
                this.Apply(emailDeleted);
                break;
            default:
                throw new ArgumentException($"Unknown event type: {@event.GetType().Name}");
        }
    }

    private void Apply(EmailDeleted @event)
    {
        this.WhenDeleted = @event.WhenDeleted;
    }

    private void Apply(EmailReceived @event)
    {
        this.Id = @event.EmailId;
        this.DomainId = @event.DomainId;
        this.SubdomainId = @event.SubdomainId;
        this.Subject = @event.Subject;
        this.WhenSent = @event.WhenSent;
        this._emailAddresses.AddRange(@event.EmailAddresses.Select(ea => new EmailAddress(ea.Address, ea.Name, ea.EmailAddressType)));
    }
}