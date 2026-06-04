using DotNetCore.CAP;
using Spamma.App.Infrastructure.Contracts.Services;
using Spamma.Modules.Common.IntegrationEvents;

namespace Spamma.App.Infrastructure.IntegrationEventSubscribers;

public class NotifySubscribersWhenSystemSettingsAreUpdated(IClientNotifierService clientNotifierService)
    : ICapSubscribe
{
    [CapSubscribe(IntegrationEventNames.SystemSettingsUpdated)]
    public Task Process(SystemSettingsUpdatedIntegrationEvent ev)
    {
        return clientNotifierService.NotifySystemSettingsUpdated(ev.Settings);
    }
}
