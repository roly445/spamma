using JasperFx.Events;
using JetBrains.Annotations;
using Marten;
using Marten.Events.Projections;
using Marten.Patching;
using Spamma.Modules.DomainManagement.Domain.DomainAggregate.Events;
using Spamma.Modules.DomainManagement.Domain.SubdomainAggregate.Events;
using Spamma.Modules.DomainManagement.Infrastructure.ReadModels;
using Spamma.Modules.UserManagement.Infrastructure.ReadModels;
using DomainModerationUserAdded = Spamma.Modules.DomainManagement.Domain.DomainAggregate.Events.ModerationUserAdded;
using DomainModerationUserRemoved = Spamma.Modules.DomainManagement.Domain.DomainAggregate.Events.ModerationUserRemoved;

namespace Spamma.Modules.DomainManagement.Infrastructure.Projections;

public class DomainLookupProjection : EventProjection
{
    [UsedImplicitly]
    public DomainLookup Create(DomainCreated @event)
    {
        return new DomainLookup
        {
            Id = @event.DomainId,
            DomainName = @event.Name,
            PrimaryContact = @event.PrimaryContactEmail,
            VerificationToken = @event.VerificationToken,
            Description = @event.Description,
            IsVerified = false,
            SubdomainCount = 0,
            AssignedModeratorCount = 0,
            CreatedAt = @event.WhenCreated,
        };
    }

    [UsedImplicitly]
    public void Project(IEvent<DomainSuspended> @event, IDocumentOperations ops)
    {
        ops.Patch<DomainLookup>(@event.StreamId)
            .Set(x => x.IsSuspended, true)
            .Set(x => x.WhenSuspended, @event.Data.WhenSuspended);
    }

    [UsedImplicitly]
    public void Project(IEvent<DomainUnsuspended> @event, IDocumentOperations ops)
    {
        ops.Patch<DomainLookup>(@event.StreamId)
            .Set(x => x.IsSuspended, false)
            .Set(x => x.WhenSuspended, null);
    }

    [UsedImplicitly]
    public void Project(IEvent<DetailsUpdated> @event, IDocumentOperations ops)
    {
        ops.Patch<DomainLookup>(@event.StreamId)
            .Set(x => x.Description, @event.Data.Description)
            .Set(x => x.PrimaryContact, @event.Data.PrimaryContactEmail);
    }

    [UsedImplicitly]
    public void Project(IEvent<DomainVerified> @event, IDocumentOperations ops)
    {
        ops.Patch<DomainLookup>(@event.StreamId)
            .Set(x => x.IsVerified, true)
            .Set(x => x.VerifiedAt, @event.Data.WhenVerified);
    }

    [UsedImplicitly]
    public async Task Project(IEvent<DomainModerationUserAdded> @event, IDocumentOperations ops)
    {
        var user = await ops.LoadAsync<UserLookup>(@event.Data.UserId);
        var name = user?.Name ?? string.Empty;
        var email = user?.EmailAddress ?? string.Empty;

        ops.Patch<UserLookup>(@event.Data.UserId)
            .Append(x => x.ModeratedDomains, @event.StreamId);

        ops.Patch<DomainLookup>(@event.StreamId)
            .Append(x => x.DomainModerators, new DomainModerator
            {
                UserId = @event.Data.UserId,
                Name = name,
                Email = email,
                CreatedAt = @event.Data.WhenAdded,
            })
            .Increment(x => x.AssignedModeratorCount);
    }

    [UsedImplicitly]
    public async Task Project(IEvent<DomainModerationUserRemoved> @event, IDocumentOperations ops)
    {
        var user = await ops.LoadAsync<UserLookup>(@event.Data.UserId);
        var name = user?.Name ?? string.Empty;
        var email = user?.EmailAddress ?? string.Empty;

        ops.Patch<UserLookup>(@event.Data.UserId)
            .Remove(x => x.ModeratedDomains, @event.StreamId);

        ops.Patch<DomainLookup>(@event.StreamId)
            .Remove(
                x => x.DomainModerators,
                dm => dm.UserId == @event.Data.UserId && dm.Name == name && dm.Email == email)
            .Increment(x => x.AssignedModeratorCount, -1);
    }

    [UsedImplicitly]
    public async Task Project(IEvent<SubdomainCreated> @event, IDocumentOperations ops)
    {
        var domain = await ops.LoadAsync<DomainLookup>(@event.Data.DomainId);
        if (domain == null)
        {
            return;
        }

        var fullName = $"{@event.Data.Name}.{domain.DomainName}";

        var subdomain = new SubdomainLookup
        {
            Id = @event.Data.SubdomainId,
            SubdomainName = @event.Data.Name,
            DomainId = @event.Data.DomainId,
            CreatedAt = @event.Data.WhenCreated,
            AssignedModeratorCount = 0,
            IsSuspended = false,
            WhenSuspended = null,
            Description = @event.Data.Description,
            ParentName = domain.DomainName,
            FullName = fullName,
            ChaosMonkeyRuleCount = 0,
            ActiveCampaignCount = 0,
        };

        ops.Patch<DomainLookup>(@event.Data.DomainId)
            .Append(x => x.Subdomains, subdomain)
            .Increment(x => x.SubdomainCount);
    }
}