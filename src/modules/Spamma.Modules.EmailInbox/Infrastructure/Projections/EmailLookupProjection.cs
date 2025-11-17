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
    public void Project(IEvent<EmailReceived> @event, IDocumentOperations ops)
    {
        ops.Insert(new EmailLookup
        {
            Id = @event.Data.EmailId,
            DomainId = @event.Data.DomainId,
            SubdomainId = @event.Data.SubdomainId,
            Subject = @event.Data.Subject,
            SentAt = @event.Data.SentAt,
        });

        var emailAddresses = @event.Data.EmailAddresses
            .Select(x => new ReadModels.EmailAddress(x.Address, x.Name, x.EmailAddressType))
            .ToList();

        foreach (var emailAddress in emailAddresses)
        {
            ops.Patch<EmailLookup>(@event.StreamId)
                .Append(x => x.EmailAddresses, emailAddress);
        }
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