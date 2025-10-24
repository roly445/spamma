using BluQube.Commands;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
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
            return CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.NotFound, $"Email with ID {request.EmailId} not found"));
        }

        var email = emailMaybe.Value;
        var result = email.Delete(timeProvider.GetUtcNow().DateTime);
        if (!result.IsSuccess)
        {
            return CommandResult.Failed(result.Error);
        }

        var saveResult = await repository.SaveAsync(email, cancellationToken);
        if (saveResult.IsFailure)
        {
            return CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.SavingChangesFailed));
        }

        await eventPublisher.PublishAsync(new EmailDeletedIntegrationEvent(email.Id), cancellationToken);

        return CommandResult.Succeeded();
    }
}