using BluQube.Commands;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.UserManagement.Application.Repositories;
using Spamma.Modules.UserManagement.Client.Application.Commands;

namespace Spamma.Modules.UserManagement.Application.CommandHandlers.User;

internal class CompleteAuthenticationCommandHandler(
    IUserRepository repository,
    TimeProvider timeProvider,
    IOptions<Settings> settings,
    IEnumerable<IValidator<CompleteAuthenticationCommand>> validators,
    ILogger<CompleteAuthenticationCommandHandler> logger) : CommandHandler<CompleteAuthenticationCommand>(validators, logger)
{
    private readonly Settings _settings = settings.Value;

    protected override async Task<CommandResult> HandleInternal(CompleteAuthenticationCommand request, CancellationToken cancellationToken)
    {
        var userMaybe = await repository.GetByIdAsync(request.UserId, cancellationToken);

        if (userMaybe.HasNoValue)
        {
            return CommandResult.Failed(new BluQube.Commands.BluQubeErrorData(CommonErrorCodes.NotFound, $"User with ID {request.UserId} not found"));
        }

        var user = userMaybe.Value;
        var authResult = user.ProcessAuthentication(
            request.AuthenticationAttemptId,
            request.SecurityStamp,
            timeProvider.GetUtcNow().UtcDateTime,
            this._settings.AuthenticationTimeInMinutes);

        if (authResult.IsFailure)
        {
            return CommandResult.Failed(authResult.Error);
        }

        var saveResult = await repository.SaveAsync(user, cancellationToken);
        return !saveResult.IsSuccess ? CommandResult.Failed(new BluQube.Commands.BluQubeErrorData(CommonErrorCodes.SavingChangesFailed)) : CommandResult.Succeeded();
    }
}