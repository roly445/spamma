using JasperFx.Events;
using JetBrains.Annotations;
using Marten;
using Marten.Events.Projections;
using Marten.Patching;
using Spamma.Modules.DomainManagement.Client.Contracts;
using Spamma.Modules.DomainManagement.Domain.SubdomainAggregate.Events;
using Spamma.Modules.DomainManagement.Infrastructure.ReadModels;
using Spamma.Modules.UserManagement.Infrastructure.ReadModels;
using SubdomainModerationUserAdded = Spamma.Modules.DomainManagement.Domain.SubdomainAggregate.Events.ModerationUserAdded;
using SubdomainModerationUserRemoved = Spamma.Modules.DomainManagement.Domain.SubdomainAggregate.Events.ModerationUserRemoved;

namespace Spamma.Modules.DomainManagement.Infrastructure.Projections;

internal class SubdomainLookupProjection : EventProjection
{
    [UsedImplicitly]
    public async Task<SubdomainLookup> Create(SubdomainCreated @event, IDocumentOperations ops)
    {
        return new SubdomainLookup();
    }

    [UsedImplicitly]
    public async Task Project(IEvent<SubdomainCreated> @event, IDocumentOperations ops)
    {
        var domain = await ops.LoadAsync<DomainLookup>(@event.Data.DomainId);
        var parentName = domain?.DomainName ?? string.Empty;
        var fullName = $"{@event.Data.Name}.{parentName}";

        ops.Patch<SubdomainLookup>(@event.StreamId)
            .Set(x => x.Id, @event.StreamId)
            .Set(x => x.SubdomainName, @event.Data.Name)
            .Set(x => x.Description, @event.Data.Description)
            .Set(x => x.AssignedModeratorCount, 0)
            .Set(x => x.CreatedAt, @event.Data.CreatedAt)
            .Set(x => x.IsSuspended, false)
            .Set(x => x.SuspendedAt, null)
            .Set(x => x.DomainId, @event.Data.DomainId)
            .Set(x => x.ActiveCampaignCount, 0)
            .Set(x => x.ChaosMonkeyRuleCount, 0)
            .Set(x => x.ParentName, parentName)
            .Set(x => x.FullName, fullName)
            .Set(x => x.AssignedViewerCount, 0)
            .Set(x => x.MxStatus, MxStatus.NotChecked)
            .Set(x => x.MxLastCheckedAt, null);
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
    public async Task Project(IEvent<SubdomainModerationUserAdded> @event, IDocumentOperations ops)
    {
        var user = await ops.LoadAsync<UserLookup>(@event.Data.UserId);
        var name = user?.Name ?? string.Empty;
        var email = user?.EmailAddress ?? string.Empty;

        ops.Patch<UserLookup>(@event.Data.UserId)
            .Append(x => x.ModeratedSubdomains, @event.StreamId);

        ops.Patch<SubdomainLookup>(@event.StreamId)
            .Append(x => x.SubdomainModerators, new SubdomainModerator
            {
                UserId = @event.Data.UserId,
                Name = name,
                Email = email,
                CreatedAt = @event.Data.AddedAt,
            })
            .Increment(x => x.AssignedModeratorCount);
    }

    [UsedImplicitly]
    public async Task Project(IEvent<SubdomainModerationUserRemoved> @event, IDocumentOperations ops)
    {
        var user = await ops.LoadAsync<UserLookup>(@event.Data.UserId);
        var name = user?.Name ?? string.Empty;
        var email = user?.EmailAddress ?? string.Empty;

        ops.Patch<UserLookup>(@event.Data.UserId)
            .Remove(x => x.ModeratedSubdomains, @event.StreamId);

        ops.Patch<SubdomainLookup>(@event.StreamId)
            .Remove(
                x => x.SubdomainModerators,
                dm => dm.UserId == @event.Data.UserId && dm.Name == name && dm.Email == email)
            .Increment(x => x.AssignedModeratorCount, -1);
    }

    [UsedImplicitly]
    public async Task Project(IEvent<ViewerAdded> @event, IDocumentOperations ops)
    {
        var user = await ops.LoadAsync<UserLookup>(@event.Data.UserId);
        var name = user?.Name ?? string.Empty;
        var email = user?.EmailAddress ?? string.Empty;

        ops.Patch<UserLookup>(@event.Data.UserId)
            .Append(x => x.ViewableSubdomains, @event.StreamId);

        ops.Patch<SubdomainLookup>(@event.StreamId)
            .Append(x => x.Viewers, new Viewer
            {
                UserId = @event.Data.UserId,
                Name = name,
                Email = email,
                CreatedAt = @event.Data.AddedAt,
            })
            .Increment(x => x.AssignedViewerCount);
    }

    [UsedImplicitly]
    public async Task Project(IEvent<ViewerRemoved> @event, IDocumentOperations ops)
    {
        var user = await ops.LoadAsync<UserLookup>(@event.Data.UserId);
        var name = user?.Name ?? string.Empty;
        var email = user?.EmailAddress ?? string.Empty;

        ops.Patch<UserLookup>(@event.Data.UserId)
            .Remove(x => x.ViewableSubdomains, @event.StreamId);

        ops.Patch<SubdomainLookup>(@event.StreamId)
            .Remove(
                x => x.Viewers,
                dm => dm.UserId == @event.Data.UserId && dm.Name == name && dm.Email == email)
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