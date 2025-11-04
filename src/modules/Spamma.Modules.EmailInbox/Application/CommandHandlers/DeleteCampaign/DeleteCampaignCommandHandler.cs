using BluQube.Commands;
using Microsoft.Extensions.Logging;

namespace Spamma.Modules.EmailInbox.Application.CommandHandlers.DeleteCampaign;

internal class DeleteCampaignCommandHandler : CommandHandler<Spamma.Modules.EmailInbox.Client.Application.Commands.DeleteCampaignCommand>
{
    public DeleteCampaignCommandHandler(IEnumerable<IValidator<Spamma.Modules.EmailInbox.Client.Application.Commands.DeleteCampaignCommand>> validators, ILogger<DeleteCampaignCommandHandler> logger)
        : base(validators, logger)
    {
    }

    public override Task<CommandResult> Handle(Spamma.Modules.EmailInbox.Client.Application.Commands.DeleteCampaignCommand command, CancellationToken cancellationToken)
    {
        // TODO: Implement deletion logic (authorization, soft/hard delete, audit event)
        return Task.FromResult(CommandResult.Ok());
    }
}
