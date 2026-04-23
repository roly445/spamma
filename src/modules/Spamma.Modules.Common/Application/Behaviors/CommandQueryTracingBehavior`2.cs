using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Spamma.Modules.Common.Application.Behaviors;

public class CommandQueryTracingBehavior<TRequest, TResponse>(
    ILogger<CommandQueryTracingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly ActivitySource _activitySource = new("Spamma.Server.Pipeline");

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var handlerName = typeof(TRequest).Name;

        using var activity = _activitySource.StartActivity(handlerName);
        activity?.SetTag("handler.type", typeof(TRequest).FullName);
        activity?.SetTag("handler.module", ExtractModuleName(typeof(TRequest).FullName));

        logger.LogDebug("[{Handler}] Starting", handlerName);

        var stopwatch = Stopwatch.StartNew();
        var response = await next();
        stopwatch.Stop();

        var status = GetStatus(response);
        activity?.SetTag("handler.status", status);

        if (status.Contains("Failed", StringComparison.OrdinalIgnoreCase) ||
            status.Contains("Error", StringComparison.OrdinalIgnoreCase))
        {
            var errorMessage = GetErrorMessage(response);
            activity?.SetStatus(ActivityStatusCode.Error, errorMessage);
        }

        logger.LogInformation("[{Handler}] Completed {Status} in {ElapsedMs}ms", handlerName, status, stopwatch.ElapsedMilliseconds);

        return response;
    }

    private static string ExtractModuleName(string? fullName)
    {
        if (string.IsNullOrEmpty(fullName))
        {
            return "Unknown";
        }

        var parts = fullName.Split('.');
        var moduleIndex = Array.IndexOf(parts, "Modules");
        return moduleIndex >= 0 && moduleIndex + 1 < parts.Length
            ? parts[moduleIndex + 1]
            : "Unknown";
    }

    private static string GetStatus(TResponse response)
    {
        try
        {
            return ((dynamic)response!).Status?.ToString() ?? "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    private static string GetErrorMessage(TResponse response)
    {
        try
        {
            return ((dynamic)response!).ErrorData?.Message ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}
