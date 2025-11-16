using BluQube.Attributes;
using BluQube.Commands;

namespace Spamma.Modules.EmailInbox.Client.Application.Commands.Campaign;

[BluQubeCommand(Path = "api/email-inbox/campaigns/delete")]
public record DeleteCampaignCommand(Guid CampaignId) : ICommand;