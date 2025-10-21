using DotNetCore.CAP;
using Spamma.App.Infrastructure.Services;
using Spamma.Modules.Common.IntegrationEvents;
using Spamma.Modules.Common.IntegrationEvents.UserManagement;

namespace Spamma.App.Infrastructure.IntegrationEventSubscribers;

public class RemoveSuspendedUserFromCache(
    ILogger<RemoveSuspendedUserFromCache> logger, UserStatusCache userStatusCache)
    : ICapSubscribe
{
    [CapSubscribe(IntegrationEventNames.UserSuspended)]
    public async Task Process(UserSuspendedIntegrationEvent ev)
    {
        var userMaybe = await userStatusCache.GetUserLookupAsync(ev.UserId);
        if (userMaybe.HasNoValue)
        {
            logger.LogWarning("User with ID {UserId} not found in cache when processing UserSuspended event", ev.UserId);
            return;
        }

        userMaybe.Value.IsSuspended = true;
        await userStatusCache.SetUserLookupAsync(userMaybe.Value);
    }
}