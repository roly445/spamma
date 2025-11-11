using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using EmailPush;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Spamma.Modules.EmailInbox.Domain.PushIntegrationAggregate;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;
using Spamma.Modules.EmailInbox.Infrastructure.Repositories;

namespace Spamma.Modules.EmailInbox.Infrastructure.Services;

/// <summary>
/// Manages push notification connections and email notifications.
/// </summary>
public class PushNotificationManager
{
    private readonly ConcurrentDictionary<string, (IServerStreamWriter<EmailNotification> Stream, Guid UserId, IEnumerable<PushIntegrationLookup> Integrations)> _activeConnections = new();

    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="PushNotificationManager"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for creating scopes.</param>
    public PushNotificationManager(IServiceProvider serviceProvider)
    {
        this._serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Registers a new push notification connection.
    /// </summary>
    /// <param name="connectionId">The connection ID.</param>
    /// <param name="stream">The server stream writer.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="context">The server call context.</param>
    /// <returns>A task.</returns>
    public async Task RegisterConnectionAsync(string connectionId, IServerStreamWriter<EmailNotification> stream, Guid userId, ServerCallContext context)
    {
        using var scope = this._serviceProvider.CreateScope();
        var pushIntegrationQueryRepository = scope.ServiceProvider.GetRequiredService<IPushIntegrationQueryRepository>();
        var integrations = await pushIntegrationQueryRepository.GetByUserIdAsync(userId);
        this._activeConnections[connectionId] = (stream, userId, integrations);

        // Handle disconnection
        context.CancellationToken.Register(() => this.UnregisterConnection(connectionId));
    }

    /// <summary>
    /// Unregisters a push notification connection.
    /// </summary>
    /// <param name="connectionId">The connection ID.</param>
    public void UnregisterConnection(string connectionId)
    {
        this._activeConnections.TryRemove(connectionId, out _);
    }

    /// <summary>
    /// Notifies connected clients of a new email.
    /// </summary>
    /// <param name="email">The email details.</param>
    /// <returns>A task.</returns>
    public async Task NotifyEmailAsync(EmailDetails email)
    {
        var notifications = new List<Task>();

        foreach (var connection in this._activeConnections.Values)
        {
            foreach (var integration in connection.Integrations)
            {
                if (integration.SubdomainId == email.SubdomainId && MatchesFilter(email, integration))
                {
                    var notification = new EmailNotification
                    {
                        Id = email.Id.ToString(),
                        From = email.From,
                        To = email.To,
                        Subject = email.Subject,
                        ReceivedAt = email.ReceivedAt.ToUnixTimeSeconds(),
                    };

                    notifications.Add(connection.Stream.WriteAsync(notification));
                }
            }
        }

        await Task.WhenAll(notifications);
    }

    private static bool MatchesFilter(EmailDetails email, PushIntegrationLookup integration)
    {
        return integration.FilterType switch
        {
            FilterType.AllEmails => true,
            FilterType.SingleEmail => email.To.Contains(integration.FilterValue ?? string.Empty, StringComparison.OrdinalIgnoreCase),
            FilterType.Regex => !string.IsNullOrEmpty(integration.FilterValue) && Regex.IsMatch(email.To, integration.FilterValue),
            _ => false,
        };
    }

    /// <summary>
    /// Email details for notification.
    /// </summary>
    public record EmailDetails(
        Guid Id,
        Guid SubdomainId,
        string From,
        string To,
        string Subject,
        string Body,
        DateTimeOffset ReceivedAt);
}