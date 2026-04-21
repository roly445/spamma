using DotNetCore.CAP;
using Marten;
using Marten.Patching;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common.IntegrationEvents;
using Spamma.Modules.Common.IntegrationEvents.DomainManagement;
using Spamma.Modules.UserManagement.Infrastructure.ReadModels;

namespace Spamma.Modules.UserManagement.Application.IntegrationEventSubscribers;

public class UserDomainMembershipEventHandler(
    IDocumentSession session,
    ILogger<UserDomainMembershipEventHandler> logger) : ICapSubscribe
{
    [CapSubscribe(IntegrationEventNames.UserAddedAsDomainModerator)]
    public async Task OnUserAddedAsDomainModerator(UserAddedAsDomainModeratorIntegrationEvent ev)
    {
        try
        {
            logger.LogInformation("Adding domain {DomainId} to user {UserId} moderated domains", ev.DomainId, ev.UserId);

            var user = await session.LoadAsync<UserLookup>(ev.UserId);
            if (user == null)
            {
                logger.LogWarning("User {UserId} not found when adding to moderated domains", ev.UserId);
                return;
            }

            session.Patch<UserLookup>(ev.UserId)
                .Append(x => x.ModeratedDomains, ev.DomainId);

            await session.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add domain {DomainId} to user {UserId} moderated domains", ev.DomainId, ev.UserId);
        }
    }

    [CapSubscribe(IntegrationEventNames.UserRemovedFromBeingDomainModerator)]
    public async Task OnUserRemovedFromBeingDomainModerator(UserRemovedFromBeingDomainModeratorIntegrationEvent ev)
    {
        try
        {
            logger.LogInformation("Removing domain {DomainId} from user {UserId} moderated domains", ev.DomainId, ev.UserId);

            var user = await session.LoadAsync<UserLookup>(ev.UserId);
            if (user == null)
            {
                logger.LogWarning("User {UserId} not found when removing from moderated domains", ev.UserId);
                return;
            }

            session.Patch<UserLookup>(ev.UserId)
                .Remove(x => x.ModeratedDomains, ev.DomainId);

            await session.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to remove domain {DomainId} from user {UserId} moderated domains", ev.DomainId, ev.UserId);
        }
    }

    [CapSubscribe(IntegrationEventNames.UserAddedAsSubdomainModerator)]
    public async Task OnUserAddedAsSubdomainModerator(UserAddedAsSubdomainModeratorIntegrationEvent ev)
    {
        try
        {
            logger.LogInformation("Adding subdomain {SubdomainId} to user {UserId} moderated subdomains", ev.SubdomainId, ev.UserId);

            var user = await session.LoadAsync<UserLookup>(ev.UserId);
            if (user == null)
            {
                logger.LogWarning("User {UserId} not found when adding to moderated subdomains", ev.UserId);
                return;
            }

            session.Patch<UserLookup>(ev.UserId)
                .Append(x => x.ModeratedSubdomains, ev.SubdomainId);

            await session.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add subdomain {SubdomainId} to user {UserId} moderated subdomains", ev.SubdomainId, ev.UserId);
        }
    }

    [CapSubscribe(IntegrationEventNames.UserRemovedFromBeingSubdomainModerator)]
    public async Task OnUserRemovedFromBeingSubdomainModerator(UserRemovedFromBeingSubdomainModeratorIntegrationEvent ev)
    {
        try
        {
            logger.LogInformation("Removing subdomain {SubdomainId} from user {UserId} moderated subdomains", ev.SubdomainId, ev.UserId);

            var user = await session.LoadAsync<UserLookup>(ev.UserId);
            if (user == null)
            {
                logger.LogWarning("User {UserId} not found when removing from moderated subdomains", ev.UserId);
                return;
            }

            session.Patch<UserLookup>(ev.UserId)
                .Remove(x => x.ModeratedSubdomains, ev.SubdomainId);

            await session.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to remove subdomain {SubdomainId} from user {UserId} moderated subdomains", ev.SubdomainId, ev.UserId);
        }
    }

    [CapSubscribe(IntegrationEventNames.UserAddedAsSubdomainViewer)]
    public async Task OnUserAddedAsSubdomainViewer(UserAddedAsSubdomainViewerIntegrationEvent ev)
    {
        try
        {
            logger.LogInformation("Adding subdomain {SubdomainId} to user {UserId} viewable subdomains", ev.SubdomainId, ev.UserId);

            var user = await session.LoadAsync<UserLookup>(ev.UserId);
            if (user == null)
            {
                logger.LogWarning("User {UserId} not found when adding to viewable subdomains", ev.UserId);
                return;
            }

            session.Patch<UserLookup>(ev.UserId)
                .Append(x => x.ViewableSubdomains, ev.SubdomainId);

            await session.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add subdomain {SubdomainId} to user {UserId} viewable subdomains", ev.SubdomainId, ev.UserId);
        }
    }

    [CapSubscribe(IntegrationEventNames.UserRemovedFromBeingSubdomainViewer)]
    public async Task OnUserRemovedFromBeingSubdomainViewer(UserRemovedFromBeingSubdomainViewerIntegrationEvent ev)
    {
        try
        {
            logger.LogInformation("Removing subdomain {SubdomainId} from user {UserId} viewable subdomains", ev.SubdomainId, ev.UserId);

            var user = await session.LoadAsync<UserLookup>(ev.UserId);
            if (user == null)
            {
                logger.LogWarning("User {UserId} not found when removing from viewable subdomains", ev.UserId);
                return;
            }

            session.Patch<UserLookup>(ev.UserId)
                .Remove(x => x.ViewableSubdomains, ev.SubdomainId);

            await session.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to remove subdomain {SubdomainId} from user {UserId} viewable subdomains", ev.SubdomainId, ev.UserId);
        }
    }
}
