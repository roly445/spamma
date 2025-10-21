using DotNetCore.CAP;
using Spamma.Modules.Common.IntegrationEvents;
using Spamma.Modules.Common.IntegrationEvents.EmailInbox;
using Spamma.Modules.EmailInbox.Infrastructure.Services;

namespace Spamma.Modules.EmailInbox.Application.IntegrationEventSubscribers;

public class DeleteFileWhenEmailIsDeleted(IMessageStoreProvider messageStoreProvider) : ICapSubscribe
{
    [CapSubscribe(IntegrationEventNames.EmailDeleted)]
    public async Task Process(EmailDeletedIntegrationEvent ev)
    {
        await messageStoreProvider.DeleteMessageContentAsync(ev.EmailId);
    }
}