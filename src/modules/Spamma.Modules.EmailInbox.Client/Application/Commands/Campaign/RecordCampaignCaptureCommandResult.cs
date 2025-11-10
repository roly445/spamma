using BluQube.Commands;

namespace Spamma.Modules.EmailInbox.Client.Application.Commands.Campaign;

public record RecordCampaignCaptureCommandResult(Guid CampaignId, bool IsFirstEmail) : ICommandResult;