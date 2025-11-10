using DotNetCore.CAP;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common.Caching;
using Spamma.Modules.Common.IntegrationEvents;
using Spamma.Modules.Common.IntegrationEvents.DomainManagement;

namespace Spamma.Modules.DomainManagement.Infrastructure.IntegrationEventHandlers;

public class CacheInvalidationEventHandler(
    ISubdomainCache subdomainCache,
    IChaosAddressCache chaosAddressCache,
    ILogger<CacheInvalidationEventHandler> logger) : ICapSubscribe
{
    [CapSubscribe(IntegrationEventNames.SubdomainStatusChanged)]
    public async Task OnSubdomainStatusChanged(SubdomainStatusChangedIntegrationEvent ev)
    {
        try
        {
            logger.LogInformation("Invalidating cache for subdomain status change: {DomainName}", ev.DomainName);
            await subdomainCache.InvalidateAsync(ev.DomainName);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to invalidate subdomain cache for {DomainName}", ev.DomainName);
        }
    }

    [CapSubscribe(IntegrationEventNames.ChaosAddressUpdated)]
    public async Task OnChaosAddressUpdated(ChaosAddressUpdatedIntegrationEvent ev)
    {
        try
        {
            logger.LogInformation("Invalidating cache for chaos address update: {SubdomainId}", ev.SubdomainId);
            await chaosAddressCache.InvalidateBySubdomainAsync(ev.SubdomainId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to invalidate chaos address cache for subdomain {SubdomainId}", ev.SubdomainId);
        }
    }
}
