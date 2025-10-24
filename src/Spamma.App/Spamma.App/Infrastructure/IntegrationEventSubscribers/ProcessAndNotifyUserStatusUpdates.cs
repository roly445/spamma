using DotNetCore.CAP;
using MaybeMonad;
using Spamma.App.Infrastructure.Contracts.Services;
using Spamma.App.Infrastructure.Services;
using Spamma.Modules.Common.IntegrationEvents;
using Spamma.Modules.Common.IntegrationEvents.DomainManagement;
using Spamma.Modules.Common.IntegrationEvents.UserManagement;

namespace Spamma.App.Infrastructure.IntegrationEventSubscribers;

public class ProcessAndNotifyUserStatusUpdates(ILogger<ProcessAndNotifyUserStatusUpdates> logger, IClientNotifierService clientNotifierService, UserStatusCache userStatusCache)
    : ICapSubscribe
{
    [CapSubscribe(IntegrationEventNames.UserUnsuspended)]
    public Task Process(UserUnsuspendedIntegrationEvent ev)
    {
        return this.DiscoverAndUpdateCache(ev.UserId);
    }

    [CapSubscribe(IntegrationEventNames.UserSuspended)]
    public Task Process(UserSuspendedIntegrationEvent ev)
    {
        return this.DiscoverAndUpdateCache(ev.UserId);
    }

    [CapSubscribe(IntegrationEventNames.UserAddedAsDomainModerator)]
    public Task Process(UserAddedAsDomainModeratorIntegrationEvent ev)
    {
        return this.DiscoverAndUpdateCache(ev.UserId);
    }

    [CapSubscribe(IntegrationEventNames.UserAddedAsSubdomainModerator)]
    public Task Process(UserAddedAsSubdomainModeratorIntegrationEvent ev)
    {
        return this.DiscoverAndUpdateCache(ev.UserId);
    }

    [CapSubscribe(IntegrationEventNames.UserAddedAsSubdomainViewer)]
    public Task Process(UserAddedAsSubdomainViewerIntegrationEvent ev)
    {
        return this.DiscoverAndUpdateCache(ev.UserId);
    }

    [CapSubscribe(IntegrationEventNames.UserRemovedFromBeingDomainModerator)]
    public Task Process(UserRemovedFromBeingDomainModeratorIntegrationEvent ev)
    {
        return this.DiscoverAndUpdateCache(ev.UserId);
    }

    [CapSubscribe(IntegrationEventNames.UserRemovedFromBeingSubdomainModerator)]
    public Task Process(UserRemovedFromBeingSubdomainModeratorIntegrationEvent ev)
    {
        return this.DiscoverAndUpdateCache(ev.UserId);
    }

    [CapSubscribe(IntegrationEventNames.UserRemovedFromBeingSubdomainViewer)]
    public Task Process(UserRemovedFromBeingSubdomainViewerIntegrationEvent ev)
    {
        return this.DiscoverAndUpdateCache(ev.UserId);
    }
    
    [CapSubscribe(IntegrationEventNames.UserDetailsUpdated)]
    public Task Process(UserDetailsUpdatedIntegrationEvent ev)
    {
        return this.DiscoverAndUpdateCache(ev.UserId);
    }

    private async Task<Maybe<UserStatusCache.CachedUser>> DiscoverAndUpdateCache(Guid userId)
    {
        var userMaybe = await userStatusCache.GetUserLookupAsync(userId, true);
        if (userMaybe.HasNoValue)
        {
            logger.LogWarning("User with ID {UserId} not found in cache when processing UserSuspended event", userId);
            return userMaybe;
        }

        userMaybe.Value.IsSuspended = false;
        await userStatusCache.SetUserLookupAsync(userMaybe.Value);
        await clientNotifierService.NotifyUserWhenUpdated(userMaybe.Value.UserId);
        return userMaybe;
    }
}