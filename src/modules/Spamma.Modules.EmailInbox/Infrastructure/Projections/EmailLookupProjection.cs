using JasperFx.Events;
using JetBrains.Annotations;
using Marten;
using Marten.Events.Projections;
using Marten.Patching;
using Spamma.Modules.EmailInbox.Domain.EmailAggregate.Events;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

namespace Spamma.Modules.EmailInbox.Infrastructure.Projections;

public class EmailLookupProjection : EventProjection
{
    [UsedImplicitly]
    public EmailLookup Create(EmailReceived @event)
    {
        var emailAddresses = @event.EmailAddresses.Select(x => new ReadModels.EmailAddress(x.Address, x.Name, x.EmailAddressType)).ToList();

        return new EmailLookup
        {
            Id = @event.EmailId,
            DomainId = @event.DomainId,
            SubdomainId = @event.SubdomainId,
            EmailAddresses = emailAddresses,
            Subject = @event.Subject,
            SentAt = @event.SentAt,
        };
    }

    [UsedImplicitly]
    public void Project(IEvent<EmailDeleted> @event, IDocumentOperations ops)
    {
        ops.Patch<EmailLookup>(@event.StreamId)
            .Set(x => x.DeletedAt, @event.Data.DeletedAt);
    }

    [UsedImplicitly]
    public void Project(IEvent<EmailMarkedAsFavorite> @event, IDocumentOperations ops)
    {
        ops.Patch<EmailLookup>(@event.StreamId)
            .Set(x => x.IsFavorite, true);
    }

    [UsedImplicitly]
    public void Project(IEvent<EmailUnmarkedAsFavorite> @event, IDocumentOperations ops)
    {
        ops.Patch<EmailLookup>(@event.StreamId)
            .Set(x => x.IsFavorite, false);
    }

    [UsedImplicitly]
    public void Project(IEvent<CampaignCaptured> @event, IDocumentOperations ops)
    {
        ops.Patch<EmailLookup>(@event.StreamId)
            .Set(x => x.CampaignId, @event.Data.CampaignId);
    }
}