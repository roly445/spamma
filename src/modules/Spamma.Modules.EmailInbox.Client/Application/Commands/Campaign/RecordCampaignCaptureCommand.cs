using BluQube.Attributes;
using BluQube.Commands;

namespace Spamma.Modules.EmailInbox.Client.Application.Commands;

[BluQubeCommand(Path = "api/email-inbox/campaigns/record-capture")]
public record RecordCampaignCaptureCommand(
    Guid SubdomainId,
    Guid MessageId,
    string CampaignValue,
    DateTimeOffset ReceivedAt) : ICommand;
