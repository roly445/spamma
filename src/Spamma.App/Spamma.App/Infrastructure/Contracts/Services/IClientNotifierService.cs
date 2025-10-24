namespace Spamma.App.Infrastructure.Contracts.Services;

public interface IClientNotifierService
{
    Task NotifyNewEmailForSubdomain(Guid subdomainId);

    Task NotifyEmailDeletedForSubdomain(Guid subdomainId);

    Task NotifyEmailUpdatedForSubdomain(Guid subdomainId);

    Task NotifyUserWhenUpdated(Guid userId);
}