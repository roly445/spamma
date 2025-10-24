using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Spamma.App.Infrastructure.Services;

namespace Spamma.App.Infrastructure.Hubs;

[Authorize]
public class NotifierHub(UserStatusCache userStatusCache, ILogger<NotifierHub> logger) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = this.GetCurrentUserId();
        if (userId.HasValue)
        {
            await this.JoinUserSubdomainGroups(userId.Value);
        }

        await base.OnConnectedAsync();
    }

    public async Task JoinUserGroup(string userId)
    {
        await this.Groups.AddToGroupAsync(this.Context.ConnectionId, $"user-{userId}");
    }

    public async Task LeaveUserGroup(string userId)
    {
        await this.Groups.RemoveFromGroupAsync(this.Context.ConnectionId, $"user-{userId}");
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }

    private async Task JoinUserSubdomainGroups(Guid userId)
    {
        try
        {
            var cachedUser = await userStatusCache.GetUserLookupAsync(userId);

            if (cachedUser.HasValue && !cachedUser.Value.IsSuspended)
            {
                // Join groups for each subdomain the user has access to
                foreach (var subdomainId in cachedUser.Value.ViewableSubdomains)
                {
                    var groupName = $"subdomain-{subdomainId}";
                    await this.Groups.AddToGroupAsync(this.Context.ConnectionId, groupName);
                    logger.LogDebug("Added connection {ConnectionId} to group {GroupName}", this.Context.ConnectionId, groupName);
                }

                // Also join a user-specific group for direct user notifications
                await this.Groups.AddToGroupAsync(this.Context.ConnectionId, $"user-{userId}");

                logger.LogInformation(
                    "User {UserId} joined {SubdomainCount} subdomain groups",
                    userId, cachedUser.Value.ModeratedSubdomains.Count);
            }
            else
            {
                logger.LogWarning("User {UserId} not found in cache or is suspended", userId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error joining subdomain groups for user {UserId}", userId);
        }
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = this.Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }

        return null;
    }
}