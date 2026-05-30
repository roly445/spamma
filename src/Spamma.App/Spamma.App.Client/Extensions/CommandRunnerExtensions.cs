using System.Diagnostics;
using BluQube.Commands;
using BluQube.Constants;

namespace Spamma.App.Client.Extensions;

public static class CommandRunnerExtensions
{
    private static readonly ActivitySource ActivitySource = new("Spamma.App.Client.Commands");

    public static async Task ExecuteAsync(
        this ICommandRunner commandRunner,
        ICommand command,
        Func<Task> onSuccess,
        Func<Dictionary<string, List<string>>, Task> onValidationErrors,
        Func<BluQubeErrorData, Task> onError,
        Func<Task> onNotAuthenticated,
        Dictionary<string, string>? additionalTags = null,
        CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity(command.GetType().Name);
        activity?.SetTag("command.type", command.GetType().Name);

        if (additionalTags != null)
        {
            foreach (var kvp in additionalTags)
            {
                activity?.SetTag(kvp.Key, kvp.Value);
            }
        }

        var result = await commandRunner.Send(command, cancellationToken);
        activity?.SetTag("command.status", result.Status.ToString());

        if (result.Status == CommandResultStatus.Succeeded)
        {
            await onSuccess();
        }
        else if (result.Status == CommandResultStatus.Invalid && result.ValidationResult is not null)
        {
            var errors = result.ValidationResult.Failures
                .GroupBy(f => f.PropertyName ?? string.Empty)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(f => f.ErrorMessage).ToList());
            await onValidationErrors(errors);
        }
        else if (result.Status == CommandResultStatus.Failed && result.ErrorData is not null && result.ErrorData.Code == BluQubeErrorCodes.NotAuthorized)
        {
            activity?.SetStatus(ActivityStatusCode.Error, result.ErrorData.Message);
            await onNotAuthenticated();
        }
        else if (result.Status == CommandResultStatus.Failed && result.ErrorData is not null)
        {
            activity?.SetStatus(ActivityStatusCode.Error, result.ErrorData.Message);
            await onError(result.ErrorData);
        }
        else
        {
            activity?.SetStatus(ActivityStatusCode.Error, "An unknown error occurred.");
            await onError(new BluQubeErrorData("UNKNOWN", "An unknown error occurred."));
        }
    }

    public static Task ExecuteAsync(
        this ICommandRunner commandRunner,
        ICommand command,
        Func<Task> onSuccess,
        Func<Dictionary<string, List<string>>, Task> onValidationErrors,
        Func<BluQubeErrorData, Task> onError,
        Dictionary<string, string>? additionalTags = null,
        CancellationToken cancellationToken = default)
        => commandRunner.ExecuteAsync(
            command,
            onSuccess,
            onValidationErrors,
            onError,
            onNotAuthenticated: () => onError(new BluQubeErrorData(BluQubeErrorCodes.NotAuthorized, "You are not authorized to perform this action.")),
            additionalTags,
            cancellationToken);

    public static async Task ExecuteAsync<TResult>(
        this ICommandRunner commandRunner,
        ICommand<TResult> command,
        Func<TResult, Task> onSuccess,
        Func<Dictionary<string, List<string>>, Task> onValidationErrors,
        Func<BluQubeErrorData, Task> onError,
        Func<Task> onNotAuthenticated,
        Dictionary<string, string>? additionalTags = null,
        CancellationToken cancellationToken = default)
        where TResult : ICommandResult
    {
        using var activity = ActivitySource.StartActivity(command.GetType().Name);
        activity?.SetTag("command.type", command.GetType().Name);

        if (additionalTags != null)
        {
            foreach (var kvp in additionalTags)
            {
                activity?.SetTag(kvp.Key, kvp.Value);
            }
        }

        var result = await commandRunner.Send(command, cancellationToken);
        activity?.SetTag("command.status", result.Status.ToString());

        if (result.Status == CommandResultStatus.Succeeded)
        {
            await onSuccess(result.Data);
        }
        else if (result.Status == CommandResultStatus.Invalid && result.ValidationResult is not null)
        {
            var errors = result.ValidationResult.Failures
                .GroupBy(f => f.PropertyName ?? string.Empty)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(f => f.ErrorMessage).ToList());
            await onValidationErrors(errors);
        }
        else if (result.Status == CommandResultStatus.Failed && result.ErrorData is not null && result.ErrorData.Code == BluQubeErrorCodes.NotAuthorized)
        {
            activity?.SetStatus(ActivityStatusCode.Error, result.ErrorData.Message);
            await onNotAuthenticated();
        }
        else if (result.Status == CommandResultStatus.Failed && result.ErrorData is not null)
        {
            activity?.SetStatus(ActivityStatusCode.Error, result.ErrorData.Message);
            await onError(result.ErrorData);
        }
        else
        {
            activity?.SetStatus(ActivityStatusCode.Error, "An unknown error occurred.");
            await onError(new BluQubeErrorData("UNKNOWN", "An unknown error occurred."));
        }
    }

    public static Task ExecuteAsync<TResult>(
        this ICommandRunner commandRunner,
        ICommand<TResult> command,
        Func<TResult, Task> onSuccess,
        Func<Dictionary<string, List<string>>, Task> onValidationErrors,
        Func<BluQubeErrorData, Task> onError,
        Dictionary<string, string>? additionalTags = null,
        CancellationToken cancellationToken = default)
        where TResult : ICommandResult
        => commandRunner.ExecuteAsync(
            command,
            onSuccess,
            onValidationErrors,
            onError,
            onNotAuthenticated: () => onError(new BluQubeErrorData(BluQubeErrorCodes.NotAuthorized, "You are not authorized to perform this action.")),
            additionalTags,
            cancellationToken);
}
