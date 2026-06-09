using Marten;
using Marten.Linq;
using Spamma.Modules.Common.Client;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

namespace Spamma.Modules.EmailInbox.Application.Authorizers.Queries;

internal static class EmailAccessAuthorizer
{
    public static async Task<bool> CanAccessAsync(
        UserAuthInfo user,
        EmailLookup email,
        IDocumentSession documentSession,
        CancellationToken cancellationToken)
    {
        if (user.SystemRole.HasFlag(SystemRole.DomainManagement) ||
            user.ModeratedDomains.Contains(email.DomainId) ||
            user.ModeratedSubdomains.Contains(email.SubdomainId) ||
            user.ViewableSubdomains.Contains(email.SubdomainId))
        {
            return true;
        }

        if (email.SubdomainId != EmailInboxSettingsDocument.CatchAllSubdomainId ||
            !email.CatchAllSenderAddressId.HasValue)
        {
            return false;
        }

        return await documentSession.Query<CatchAllSenderAddressLookup>()
            .AnyAsync(
                x => x.Id == email.CatchAllSenderAddressId.Value &&
                     x.AssignedUserIds.Contains(user.UserId) &&
                     !x.IsRemoved,
                cancellationToken);
    }
}
