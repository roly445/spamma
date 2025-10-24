using DotNetCore.CAP;
using Spamma.App.Infrastructure.Contracts.Services;
using Spamma.Modules.Common.IntegrationEvents;
using Spamma.Modules.Common.IntegrationEvents.EmailInbox;

namespace Spamma.App.Infrastructure.IntegrationEventSubscribers;

public class NotifySubscribersWhenEmailIsReceived(IClientNotifierService clientNotifierService)
    : ICapSubscribe
{
    [CapSubscribe(IntegrationEventNames.EmailReceived)]
    public Task Process(EmailReceivedIntegrationEvent ev)
    {
        return clientNotifierService.NotifyNewEmailForSubdomain(ev.SubdomainId);
    }
}