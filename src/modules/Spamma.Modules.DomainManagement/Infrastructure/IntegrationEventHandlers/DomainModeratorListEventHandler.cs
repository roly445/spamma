using DotNetCore.CAP;
using Marten;
using Marten.Patching;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common.IntegrationEvents;
using Spamma.Modules.Common.IntegrationEvents.DomainManagement;
using Spamma.Modules.DomainManagement.Infrastructure.ReadModels;

namespace Spamma.Modules.DomainManagement.Infrastructure.IntegrationEventHandlers;

public class DomainModeratorListEventHandler(
    IDocumentSession session,
    ILogger<DomainModeratorListEventHandler> logger) : ICapSubscribe
{
    [CapSubscribe(IntegrationEventNames.UserAddedAsDomainModerator)]
    public async Task OnUserAddedAsDomainModerator(UserAddedAsDomainModeratorIntegrationEvent ev)
    {
        try
        {
            logger.LogInformation("Adding domain moderator {UserId} to domain {DomainId}", ev.UserId, ev.DomainId);

            var domain = await session.LoadAsync<DomainLookup>(ev.DomainId);
            if (domain == null)
            {
                logger.LogWarning("Domain {DomainId} not found when adding moderator", ev.DomainId);
                return;
            }

            session.Patch<DomainLookup>(ev.DomainId)
                .Append(x => x.DomainModerators, new DomainModerator
                {
                    UserId = ev.UserId,
                    Name = ev.UserName,
                    Email = ev.UserEmail,
                    CreatedAt = DateTime.UtcNow,
                });

            await session.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add domain moderator {UserId} to domain {DomainId}", ev.UserId, ev.DomainId);
        }
    }

    [CapSubscribe(IntegrationEventNames.UserRemovedFromBeingDomainModerator)]
    public async Task OnUserRemovedFromBeingDomainModerator(UserRemovedFromBeingDomainModeratorIntegrationEvent ev)
    {
        try
        {
            logger.LogInformation("Removing domain moderator {UserId} from domain {DomainId}", ev.UserId, ev.DomainId);

            var domain = await session.LoadAsync<DomainLookup>(ev.DomainId);
            if (domain == null)
            {
                logger.LogWarning("Domain {DomainId} not found when removing moderator", ev.DomainId);
                return;
            }

            session.Patch<DomainLookup>(ev.DomainId)
                .Remove(x => x.DomainModerators, dm => dm.UserId == ev.UserId);

            await session.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to remove domain moderator {UserId} from domain {DomainId}", ev.UserId, ev.DomainId);
        }
    }
}
