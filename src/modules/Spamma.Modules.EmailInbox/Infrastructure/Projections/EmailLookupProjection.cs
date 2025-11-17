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
        return new EmailLookup();
    }

    [UsedImplicitly]
    public void Project(IEvent<EmailReceived> @event, IDocumentOperations ops)
    {
        var emailAddresses = @event.Data.EmailAddresses.Select(x => new ReadModels.EmailAddress(x.Address, x.Name, x.EmailAddressType)).ToList();

        ops.Patch<EmailLookup>(@event.StreamId)
            .Set(x => x.Id, @event.StreamId)
            .Set(x => x.DomainId, @event.Data.DomainId)
            .Set(x => x.SubdomainId, @event.Data.SubdomainId)
            .Set(x => x.EmailAddresses, emailAddresses)
            .Set(x => x.Subject, @event.Data.Subject)
            .Set(x => x.SentAt, @event.Data.SentAt);
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