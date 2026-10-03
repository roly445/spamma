using Marten;
using Microsoft.Extensions.Options;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Client;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;
using Spamma.Modules.UserManagement.Infrastructure.ReadModels;

namespace Spamma.Modules.EmailInbox.Infrastructure.Services;

internal sealed class EmailNotificationAccessService(IDocumentSession session, IOptions<Spamma.Modules.Common.Settings> settings) : IEmailNotificationAccessService
{
    public async Task<bool> CanAccessNotificationAsync(Guid userId, PushNotificationManager.EmailDetails email, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            return false;
        }

        var user = await session.LoadAsync<UserLookup>(userId, cancellationToken);
        if (user is null || user.IsSuspended)
        {
            return false;
        }

        if (email.DomainId == Guid.Empty)
        {
            return false;
        }

        return await this.CanAccessAsync(user, email.DomainId,
            email.SubdomainId, email.CatchAllSenderAddressId, cancellationToken);
    }

    public async Task<bool> CanAccessEmailAsync(Guid userId, Guid emailId, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            return false;
        }

        var user = await session.LoadAsync<UserLookup>(userId, cancellationToken);
        var email = await session.LoadAsync<EmailLookup>(emailId, cancellationToken);
        if (user is null || user.IsSuspended || email is null || email.DeletedAt.HasValue)
        {
            return false;
        }

        return await this.CanAccessAsync(user, email.DomainId, email.SubdomainId,
            email.CatchAllSenderAddressId, cancellationToken);
    }

    private async Task<bool> CanAccessAsync(UserLookup user, Guid domainId, Guid subdomainId,
        Guid? catchAllSenderAddressId, CancellationToken cancellationToken)
    {
        if (user.Id == settings.Value.PrimaryUserId ||
            user.SystemRole.HasFlag(SystemRole.DomainManagement) ||
            user.ModeratedDomains.Contains(domainId) ||
            user.ModeratedSubdomains.Contains(subdomainId) ||
            user.ViewableSubdomains.Contains(subdomainId))
        {
            return true;
        }

        if (subdomainId != EmailInboxSettingsDocument.CatchAllSubdomainId || !catchAllSenderAddressId.HasValue)
        {
            return false;
        }

        var sender = await session.LoadAsync<CatchAllSenderAddressLookup>(catchAllSenderAddressId.Value, cancellationToken);
        return sender is not null && !sender.IsRemoved && sender.AssignedUserIds.Contains(user.Id);
    }
}
