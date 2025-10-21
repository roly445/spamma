using BluQube.Commands;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Spamma.Modules.EmailInbox.Application.Repositories;
using Spamma.Modules.EmailInbox.Client.Application.Commands;
using Spamma.Modules.EmailInbox.Domain.EmailAggregate;
using Spamma.Modules.EmailInbox.Domain.EmailAggregate.Events;

namespace Spamma.Modules.EmailInbox.Application.CommandHandlers;

public class ReceivedEmailCommandHandler(
    IEnumerable<IValidator<ReceivedEmailCommand>> validators, ILogger<ReceivedEmailCommandHandler> logger, IEmailRepository repository)
    : CommandHandler<ReceivedEmailCommand>(validators, logger)
{
    protected override async Task<CommandResult> HandleInternal(ReceivedEmailCommand request, CancellationToken cancellationToken)
    {
        var emailResult = Email.Create(
            request.EmailId, request.DomainId, request.SubdomainId, request.Subject, request.WhenSent, request.EmailAddresses.Select(x => new EmailReceived.EmailAddress(x.Address, x.Name, x.EmailAddressType)).ToList());

        if (emailResult.IsFailure)
        {
            return CommandResult.Failed(new BluQubeErrorData("SAVING_CHANGE"));
        }

        var domain = emailResult.Value;
        var saveResult = await repository.SaveAsync(domain, cancellationToken);
        return !saveResult.IsSuccess ?
            CommandResult.Failed(new BluQubeErrorData("SAVING_CHANGES")) :
            CommandResult.Succeeded();
    }
}