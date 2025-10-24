using Microsoft.AspNetCore.SignalR;
using Spamma.App.Infrastructure.Contracts.Services;
using Spamma.App.Infrastructure.Hubs;

namespace Spamma.App.Infrastructure.Services;

public class ClientNotifierService(IHubContext<NotifierHub> hubContext, ILogger<ClientNotifierService> logger)
    : IClientNotifierService
{
    public async Task NotifyNewEmailForSubdomain(Guid subdomainId)
    {
        var groupName = $"subdomain-{subdomainId}";

        try
        {
            await hubContext.Clients.Group(groupName)
                .SendAsync("NewEmailReceived");

            logger.LogDebug("Sent new email notification to group {GroupName}", groupName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send new email notification to subdomain {SubdomainId}", subdomainId);
        }
    }

    public async Task NotifyEmailDeletedForSubdomain(Guid subdomainId)
    {
        var groupName = $"subdomain-{subdomainId}";

        try
        {
            await hubContext.Clients.Group(groupName)
                .SendAsync("EmailDeleted");

            logger.LogDebug("Sent email deleted notification to group {GroupName}", groupName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email deleted notification to subdomain {SubdomainId}", subdomainId);
        }
    }

    public async Task NotifyEmailUpdatedForSubdomain(Guid subdomainId)
    {
        var groupName = $"subdomain-{subdomainId}";

        try
        {
            await hubContext.Clients.Group(groupName)
                .SendAsync("EmailUpdated");

            logger.LogDebug("Sent email updated notification to group {GroupName}", groupName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email updated notification to subdomain {SubdomainId}", subdomainId);
        }
    }

    public async Task NotifyUserWhenUpdated(Guid userId)
    {
        try
        {
            await hubContext.Clients.User(userId.ToString()).SendAsync("PermissionsUpdated");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send user updated notification to subdomain {UserId}", userId);
        }
    }
}