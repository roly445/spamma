using Spamma.Modules.Common.Client.Application.Queries;

namespace Spamma.App.Infrastructure.Contracts.Services;

public interface IClientNotifierService
{
    Task NotifyNewEmailForSubdomain(Guid subdomainId);

    Task NotifyNewCatchAllEmail();

    Task NotifyEmailDeletedForSubdomain(Guid subdomainId);

    Task NotifyEmailUpdatedForSubdomain(Guid subdomainId);

    Task NotifyUserWhenUpdated(Guid userId);

    Task NotifySystemSettingsUpdated(GetSystemSettingsQueryResult settings);
}
