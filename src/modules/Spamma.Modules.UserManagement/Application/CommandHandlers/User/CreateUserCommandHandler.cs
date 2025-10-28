using BluQube.Commands;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.Common.IntegrationEvents.UserManagement;
using Spamma.Modules.UserManagement.Application.Repositories;
using Spamma.Modules.UserManagement.Client.Application.Commands;
using UserAggregate = Spamma.Modules.UserManagement.Domain.UserAggregate.User;

namespace Spamma.Modules.UserManagement.Application.CommandHandlers.User;

internal class CreateUserCommandHandler(
    IUserRepository repository, IIntegrationEventPublisher integrationEventPublisher, TimeProvider timeProvider,
    IEnumerable<IValidator<CreateUserCommand>> validators, ILogger<CreateUserCommandHandler> logger) : CommandHandler<CreateUserCommand>(validators, logger)
{
    protected override async Task<CommandResult> HandleInternal(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var securityStamp = Guid.NewGuid();
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var createResult = UserAggregate.Create(request.UserId, request.Name, request.EmailAddress, securityStamp, now, request.SystemRole);

        if (createResult.IsFailure)
        {
            return CommandResult.Failed(createResult.Error);
        }

        var user = createResult.Value;

        var result = await repository.SaveAsync(user, cancellationToken);
        if (!result.IsSuccess)
        {
            return CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.SavingChangesFailed));
        }

        var email = user.EmailAddress;
        await integrationEventPublisher.PublishAsync(
            new UserCreatedIntegrationEvent(
            user.Id,
            user.Name,
            email,
            now, request.SendWelcome), cancellationToken);
        return CommandResult.Succeeded();
    }
}