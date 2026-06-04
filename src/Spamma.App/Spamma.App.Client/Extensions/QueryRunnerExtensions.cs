using System.Diagnostics;
using BluQube.Constants;
using BluQube.Queries;

namespace Spamma.App.Client.Extensions;

public static class QueryRunnerExtensions
{
    private static readonly ActivitySource ActivitySource = new("Spamma.App.Client.Queries");

    public static async Task ExecuteAsync<TQuery, TResult>(
        this IQueryRunner queryRunner,
        TQuery query,
        Func<TResult, Task> onSuccess,
        Func<string, Task> onError,
        Func<Task> onNotAuthenticated,
        Dictionary<string, string>? additionalTags = null,
        CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResult>
        where TResult : IQueryResult
    {
        using var activity = ActivitySource.StartActivity(query.GetType().Name);
        activity?.SetTag("query.type", query.GetType().Name);

        if (additionalTags != null)
        {
            foreach (var kvp in additionalTags)
            {
                activity?.SetTag(kvp.Key, kvp.Value);
            }
        }

        var result = await queryRunner.Send(query, cancellationToken);
        activity?.SetTag("query.status", result.Status.ToString());

        if (result.Status == QueryResultStatus.Succeeded)
        {
            await onSuccess(result.Data);
        }
        else if (result.Status == QueryResultStatus.Failed)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Query failed to execute.");
            await onError("Query failed to execute.");
        }
        else if (result.Status == QueryResultStatus.Unauthorized)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Query unauthorized.");
            await onNotAuthenticated();
        }
        else
        {
            activity?.SetStatus(ActivityStatusCode.Error, "An unknown error occurred.");
            await onError("An unknown error occurred.");
        }
    }

    public static Task ExecuteAsync<TQuery, TResult>(
        this IQueryRunner queryRunner,
        TQuery query,
        Func<TResult, Task> onSuccess,
        Func<string, Task> onError,
        Dictionary<string, string>? additionalTags = null,
        CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResult>
        where TResult : IQueryResult
        => queryRunner.ExecuteAsync(
            query,
            onSuccess,
            onError,
            onNotAuthenticated: () => onError("You are not authorized to perform this action."),
            additionalTags,
            cancellationToken);
}
