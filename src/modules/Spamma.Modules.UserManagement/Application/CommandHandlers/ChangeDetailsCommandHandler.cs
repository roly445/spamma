using BluQube.Commands;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common.Client;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.UserManagement.Application.Repositories;
using Spamma.Modules.UserManagement.Client.Application.Commands;

namespace Spamma.Modules.UserManagement.Application.CommandHandlers;

internal class ChangeDetailsCommandHandler(
    IUserRepository repository, IEnumerable<IValidator<ChangeDetailsCommand>> validators, ILogger<ChangeDetailsCommandHandler> logger) : CommandHandler<ChangeDetailsCommand>(validators, logger)
{
    protected override async Task<CommandResult> HandleInternal(ChangeDetailsCommand request, CancellationToken cancellationToken)
    {
        var userMaybe = await repository.GetByIdAsync(request.UserId, cancellationToken);

        if (userMaybe.HasNoValue)
        {
            return CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.NotFound, $"User with ID {request.UserId} not found"));
        }

        var user = userMaybe.Value;
        var updateResult = user.ChangeDetails(request.EmailAddress, request.Name, request.SystemRole);
        if (updateResult.IsFailure)
        {
            return CommandResult.Failed(updateResult.Error);
        }

        var saveResult = await repository.SaveAsync(user, cancellationToken);
        return !saveResult.IsSuccess ? CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.SavingChangesFailed)) : CommandResult.Succeeded();
    }
}