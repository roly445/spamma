using BluQube.Commands;

namespace Spamma.Modules.EmailInbox.Client.Application.Commands.Campaign;

public record RecordCampaignCaptureCommand(
    Guid DomainId,
    Guid SubdomainId,
    Guid MessageId,
    string CampaignValue,
    DateTimeOffset ReceivedAt) : ICommand<RecordCampaignCaptureCommandResult>;
