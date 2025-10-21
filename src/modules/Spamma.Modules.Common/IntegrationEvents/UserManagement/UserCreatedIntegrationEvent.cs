using Spamma.Modules.Common.Domain.Contracts;

namespace Spamma.Modules.Common.IntegrationEvents.UserManagement;

public record UserCreatedIntegrationEvent(
    Guid UserId,
    string Name,
    string EmailAddress,
    DateTime WhenHappened,
    bool SendWelcome) : IIntegrationEvent
{
    public string EventName => IntegrationEventNames.UserCreated;
}