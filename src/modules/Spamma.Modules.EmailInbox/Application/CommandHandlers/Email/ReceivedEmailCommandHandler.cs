using BluQube.Commands;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.EmailInbox.Application.Repositories;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Email;
using Spamma.Modules.EmailInbox.Domain.EmailAggregate.Events;

namespace Spamma.Modules.EmailInbox.Application.CommandHandlers.Email;

public class ReceivedEmailCommandHandler(
    IEnumerable<IValidator<ReceivedEmailCommand>> validators, ILogger<ReceivedEmailCommandHandler> logger, IEmailRepository repository)
    : CommandHandler<ReceivedEmailCommand>(validators, logger)
{
    protected override async Task<CommandResult> HandleInternal(ReceivedEmailCommand request, CancellationToken cancellationToken)
    {
        var emailResult = Domain.EmailAggregate.Email.Create(
            request.EmailId, request.DomainId, request.SubdomainId, request.Subject, request.WhenSent, request.EmailAddresses.Select(x => new EmailReceived.EmailAddress(x.Address, x.Name, x.EmailAddressType)).ToList());

        if (emailResult.IsFailure)
        {
            return CommandResult.Failed(emailResult.Error);
        }

        var domain = emailResult.Value;
        var saveResult = await repository.SaveAsync(domain, cancellationToken);
        if (saveResult.IsFailure)
        {
            return CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.SavingChangesFailed));
        }

        return CommandResult.Succeeded();
    }
}