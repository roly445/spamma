using JasperFx.Events;
using JetBrains.Annotations;
using Marten;
using Marten.Events.Projections;
using Marten.Patching;
using Spamma.Modules.DomainManagement.Client.Contracts;
using Spamma.Modules.DomainManagement.Domain.SubdomainAggregate.Events;
using Spamma.Modules.DomainManagement.Infrastructure.ReadModels;
using SubdomainModerationUserAdded = Spamma.Modules.DomainManagement.Domain.SubdomainAggregate.Events.ModerationUserAdded;
using SubdomainModerationUserRemoved = Spamma.Modules.DomainManagement.Domain.SubdomainAggregate.Events.ModerationUserRemoved;

namespace Spamma.Modules.DomainManagement.Infrastructure.Projections;

internal class SubdomainLookupProjection : EventProjection
{
    [UsedImplicitly]
    public async Task<SubdomainLookup> Create(SubdomainCreated @event, IDocumentOperations ops)
    {
        var domain = await ops.LoadAsync<DomainLookup>(@event.DomainId);
        var parentName = domain?.DomainName ?? string.Empty;
        var fullName = $"{@event.Name}.{parentName}";

        return new SubdomainLookup
        {
            Id = @event.SubdomainId,
            SubdomainName = @event.Name,
            Description = @event.Description,
            AssignedModeratorCount = 0,
            CreatedAt = @event.CreatedAt,
            IsSuspended = false,
            SuspendedAt = null,
            DomainId = @event.DomainId,
            ActiveCampaignCount = 0,
            ChaosMonkeyRuleCount = 0,
            ParentName = parentName,
            FullName = fullName,
            AssignedViewerCount = 0,
            MxStatus = MxStatus.NotChecked,
            MxLastCheckedAt = null,
        };
    }

    [UsedImplicitly]
    public void Project(IEvent<SubdomainSuspended> @event, IDocumentOperations ops)
    {
        ops.Patch<SubdomainLookup>(@event.StreamId)
            .Set(x => x.IsSuspended, true)
            .Set(x => x.SuspendedAt, @event.Data.SuspendedAt);
    }

    [UsedImplicitly]
    public void Project(IEvent<SubdomainUnsuspended> @event, IDocumentOperations ops)
    {
        ops.Patch<SubdomainLookup>(@event.StreamId)
            .Set(x => x.IsSuspended, false)
            .Set(x => x.SuspendedAt, null);
    }

    [UsedImplicitly]
    public void Project(IEvent<SubdomainUpdated> @event, IDocumentOperations ops)
    {
        ops.Patch<SubdomainLookup>(@event.StreamId)
            .Set(x => x.Description, @event.Data.Description);
    }

    [UsedImplicitly]
    public void Project(IEvent<SubdomainModerationUserAdded> @event, IDocumentOperations ops)
    {
        ops.Patch<SubdomainLookup>(@event.StreamId)
            .Increment(x => x.AssignedModeratorCount);
    }

    [UsedImplicitly]
    public void Project(IEvent<SubdomainModerationUserRemoved> @event, IDocumentOperations ops)
    {
        ops.Patch<SubdomainLookup>(@event.StreamId)
            .Increment(x => x.AssignedModeratorCount, -1);
    }

    [UsedImplicitly]
    public void Project(IEvent<ViewerAdded> @event, IDocumentOperations ops)
    {
        ops.Patch<SubdomainLookup>(@event.StreamId)
            .Increment(x => x.AssignedViewerCount);
    }

    [UsedImplicitly]
    public void Project(IEvent<ViewerRemoved> @event, IDocumentOperations ops)
    {
        ops.Patch<SubdomainLookup>(@event.StreamId)
            .Increment(x => x.AssignedViewerCount, -1);
    }

    [UsedImplicitly]
    public void Project(IEvent<MxRecordChecked> @event, IDocumentOperations ops)
    {
        ops.Patch<SubdomainLookup>(@event.StreamId)
            .Set(x => x.MxLastCheckedAt, @event.Data.LastCheckedAt)
            .Set(x => x.MxStatus, @event.Data.MxStatus);
    }
}