using DotNetCore.CAP;
using Spamma.App.Infrastructure.Services;
using Spamma.Modules.Common.IntegrationEvents;
using Spamma.Modules.Common.IntegrationEvents.UserManagement;

namespace Spamma.App.Infrastructure.IntegrationEventSubscribers;

public class AddUnsuspendedUserFromCache(
    ILogger<AddUnsuspendedUserFromCache> logger, UserStatusCache userStatusCache)
    : ICapSubscribe
{
    [CapSubscribe(IntegrationEventNames.UserUnsuspended)]
    public async Task Process(UserUnsuspendedIntegrationEvent ev)
    {
        var userMaybe = await userStatusCache.GetUserLookupAsync(ev.UserId);
        if (userMaybe.HasNoValue)
        {
            logger.LogWarning("User with ID {UserId} not found in cache when processing UserSuspended event", ev.UserId);
            return;
        }

        userMaybe.Value.IsSuspended = false;
        await userStatusCache.SetUserLookupAsync(userMaybe.Value);
    }
}