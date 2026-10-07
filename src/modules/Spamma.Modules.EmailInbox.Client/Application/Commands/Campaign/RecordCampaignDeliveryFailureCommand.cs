using BluQube.Commands;

namespace Spamma.Modules.EmailInbox.Client.Application.Commands.Campaign;

public record RecordCampaignDeliveryFailureCommand(
    Guid DomainId,
    Guid SubdomainId,
    string CampaignValue,
    Guid FailureId,
    Guid AttemptId,
    int SmtpCode,
    DateTimeOffset ObservedAt) : ICommand;
