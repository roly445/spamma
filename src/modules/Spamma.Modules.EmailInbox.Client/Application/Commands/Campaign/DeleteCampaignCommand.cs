using BluQube.Attributes;
using BluQube.Commands;

namespace Spamma.Modules.EmailInbox.Client.Application.Commands;

[BluQubeCommand(Path = "api/email-inbox/campaigns/delete")]
public record DeleteCampaignCommand(
    Guid SubdomainId,
    string CampaignValue,
    bool Force = false) : ICommand;
