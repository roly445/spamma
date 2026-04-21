using DotNetCore.CAP;
using Marten;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common.IntegrationEvents;
using Spamma.Modules.Common.IntegrationEvents.DomainManagement;
using Spamma.Modules.DomainManagement.Infrastructure.ReadModels;

namespace Spamma.Modules.DomainManagement.Infrastructure.IntegrationEventHandlers;

public class SubdomainModeratorListEventHandler(
    IDocumentSession session,
    ILogger<SubdomainModeratorListEventHandler> logger) : ICapSubscribe
{
    [CapSubscribe(IntegrationEventNames.UserAddedAsSubdomainModerator)]
    public async Task OnUserAddedAsSubdomainModerator(UserAddedAsSubdomainModeratorIntegrationEvent ev)
    {
        try
        {
            logger.LogInformation("Adding subdomain moderator {UserId} to subdomain {SubdomainId}", ev.UserId, ev.SubdomainId);

            var subdomain = await session.LoadAsync<SubdomainLookup>(ev.SubdomainId);
            if (subdomain == null)
            {
                logger.LogWarning("Subdomain {SubdomainId} not found when adding moderator", ev.SubdomainId);
                return;
            }

            session.Patch<SubdomainLookup>(ev.SubdomainId)
                .Append(x => x.SubdomainModerators, new SubdomainModerator
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
            logger.LogError(ex, "Failed to add subdomain moderator {UserId} to subdomain {SubdomainId}", ev.UserId, ev.SubdomainId);
        }
    }

    [CapSubscribe(IntegrationEventNames.UserRemovedFromBeingSubdomainModerator)]
    public async Task OnUserRemovedFromBeingSubdomainModerator(UserRemovedFromBeingSubdomainModeratorIntegrationEvent ev)
    {
        try
        {
            logger.LogInformation("Removing subdomain moderator {UserId} from subdomain {SubdomainId}", ev.UserId, ev.SubdomainId);

            var subdomain = await session.LoadAsync<SubdomainLookup>(ev.SubdomainId);
            if (subdomain == null)
            {
                logger.LogWarning("Subdomain {SubdomainId} not found when removing moderator", ev.SubdomainId);
                return;
            }

            session.Patch<SubdomainLookup>(ev.SubdomainId)
                .Remove(x => x.SubdomainModerators, dm => dm.UserId == ev.UserId);

            await session.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to remove subdomain moderator {UserId} from subdomain {SubdomainId}", ev.UserId, ev.SubdomainId);
        }
    }

    [CapSubscribe(IntegrationEventNames.UserAddedAsSubdomainViewer)]
    public async Task OnUserAddedAsSubdomainViewer(UserAddedAsSubdomainViewerIntegrationEvent ev)
    {
        try
        {
            logger.LogInformation("Adding subdomain viewer {UserId} to subdomain {SubdomainId}", ev.UserId, ev.SubdomainId);

            var subdomain = await session.LoadAsync<SubdomainLookup>(ev.SubdomainId);
            if (subdomain == null)
            {
                logger.LogWarning("Subdomain {SubdomainId} not found when adding viewer", ev.SubdomainId);
                return;
            }

            session.Patch<SubdomainLookup>(ev.SubdomainId)
                .Append(x => x.Viewers, new Viewer
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
            logger.LogError(ex, "Failed to add subdomain viewer {UserId} to subdomain {SubdomainId}", ev.UserId, ev.SubdomainId);
        }
    }

    [CapSubscribe(IntegrationEventNames.UserRemovedFromBeingSubdomainViewer)]
    public async Task OnUserRemovedFromBeingSubdomainViewer(UserRemovedFromBeingSubdomainViewerIntegrationEvent ev)
    {
        try
        {
            logger.LogInformation("Removing subdomain viewer {UserId} from subdomain {SubdomainId}", ev.UserId, ev.SubdomainId);

            var subdomain = await session.LoadAsync<SubdomainLookup>(ev.SubdomainId);
            if (subdomain == null)
            {
                logger.LogWarning("Subdomain {SubdomainId} not found when removing viewer", ev.SubdomainId);
                return;
            }

            session.Patch<SubdomainLookup>(ev.SubdomainId)
                .Remove(x => x.Viewers, v => v.UserId == ev.UserId);

            await session.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to remove subdomain viewer {UserId} from subdomain {SubdomainId}", ev.UserId, ev.SubdomainId);
        }
    }
}
