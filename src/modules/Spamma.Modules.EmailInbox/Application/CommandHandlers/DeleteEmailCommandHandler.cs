using BluQube.Commands;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.Common.IntegrationEvents.EmailInbox;
using Spamma.Modules.EmailInbox.Application.Repositories;
using Spamma.Modules.EmailInbox.Client.Application.Commands;

namespace Spamma.Modules.EmailInbox.Application.CommandHandlers;

public class DeleteEmailCommandHandler(
    IEmailRepository repository, TimeProvider timeProvider, IIntegrationEventPublisher eventPublisher,
    IEnumerable<IValidator<DeleteEmailCommand>> validators, ILogger<DeleteEmailCommandHandler> logger) : CommandHandler<DeleteEmailCommand>(validators, logger)
{
    protected override async Task<CommandResult> HandleInternal(DeleteEmailCommand request, CancellationToken cancellationToken)
    {
        var emailMaybe = await repository.GetByIdAsync(request.EmailId, cancellationToken);

        if (emailMaybe.HasNoValue)
        {
            return CommandResult.Failed(new BluQubeErrorData("EMAIL_NOT_FOUND", "The user with the provided email address does not exist."));
        }

        var email = emailMaybe.Value;
        var result = email.Delete(timeProvider.GetUtcNow().DateTime);
        if (!result.IsSuccess)
        {
            return CommandResult.Failed(new BluQubeErrorData("EMAIL_DELETION_FAILED"));
        }

        var saveResult = await repository.SaveAsync(email, cancellationToken);
        if (saveResult.IsFailure)
        {
            return CommandResult.Failed(new BluQubeErrorData("SAVING_CHANGES"));
        }

        await eventPublisher.PublishAsync(new EmailDeletedIntegrationEvent(email.Id), cancellationToken);

        return CommandResult.Succeeded();
    }
}