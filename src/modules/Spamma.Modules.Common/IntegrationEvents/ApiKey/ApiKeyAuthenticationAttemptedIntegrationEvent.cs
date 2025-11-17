using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.Common.IntegrationEvents;

namespace Spamma.Modules.Common.IntegrationEvents.ApiKey;

public record ApiKeyAuthenticationAttemptedIntegrationEvent(
    Guid ApiKeyId,
    string MaskedApiKey,
    string AuthenticationMethod,
    bool IsSuccessful,
    string? FailureReason,
    string? IpAddress,
    string? UserAgent,
    DateTimeOffset AttemptedAt) : IIntegrationEvent
{
    public string EventName => IntegrationEventNames.ApiKeyAuthenticationAttempted;
}