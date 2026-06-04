using Spamma.Modules.Common.Client.Application.Queries;
using Spamma.Modules.Common.Domain.Contracts;

namespace Spamma.Modules.Common.IntegrationEvents;

public record SystemSettingsUpdatedIntegrationEvent(GetSystemSettingsQueryResult Settings) : IIntegrationEvent
{
    public string EventName => IntegrationEventNames.SystemSettingsUpdated;
}
