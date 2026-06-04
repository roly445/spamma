using System.Collections.Concurrent;
using Grpc.Core;
using Spamma.Modules.EmailInbox.Infrastructure.Constants;

namespace Spamma.Modules.EmailInbox.Infrastructure.Services;

public class PushNotificationManager
{
    private readonly ConcurrentDictionary<string, (IServerStreamWriter<global::Spamma.Modules.EmailInbox.Client.Application.Grpc.EmailNotification> Stream, Guid UserId)> _activeConnections = new();
    private object? _clientNotifier;

    public void SetClientNotifier(object clientNotifier)
    {
        this._clientNotifier = clientNotifier;
    }

    public Task RegisterConnectionAsync(
        string connectionId,
        IServerStreamWriter<global::Spamma.Modules.EmailInbox.Client.Application.Grpc.EmailNotification> stream,
        Guid userId,
        ServerCallContext context)
    {
        this._activeConnections[connectionId] = (stream, userId);

        // Handle disconnection
        context.CancellationToken.Register(() => this.UnregisterConnection(connectionId));

        return Task.CompletedTask;
    }

    public void UnregisterConnection(string connectionId)
    {
        this._activeConnections.TryRemove(connectionId, out _);
    }

    public async Task NotifyEmailAsync(EmailDetails email, CancellationToken cancellationToken = default)
    {
        var notifications = new List<Task>();

        // Notify all active gRPC client connections
        foreach (var connection in this._activeConnections.Values)
        {
            var notification = new global::Spamma.Modules.EmailInbox.Client.Application.Grpc.EmailNotification
            {
                Id = email.Id.ToString(),
                From = email.From,
                To = email.To,
                Subject = email.Subject,
                ReceivedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(email.ReceivedAt),
                SubdomainId = email.SubdomainId.ToString(),
                IsCatchAll = email.IsCatchAll,
                CampaignId = email.CampaignId?.ToString() ?? string.Empty,
                CampaignValue = email.CampaignValue ?? string.Empty,
            };

            notifications.Add(connection.Stream.WriteAsync(notification, cancellationToken));
        }

        // Notify SignalR web clients via IClientNotifierService
        if (this._clientNotifier != null)
        {
            var methodName = email.SubdomainId == CatchAllConstants.SubdomainId
                ? "NotifyNewCatchAllEmail"
                : "NotifyNewEmailForSubdomain";

            var method = this._clientNotifier.GetType().GetMethod(methodName);
            if (method != null)
            {
                var parameters = email.SubdomainId == CatchAllConstants.SubdomainId
                    ? []
                    : new object[] { email.SubdomainId };

                var task = method.Invoke(this._clientNotifier, parameters) as Task;
                if (task != null)
                {
                    notifications.Add(task);
                }
            }
        }

        await Task.WhenAll(notifications);
    }

    public record EmailDetails(
        Guid Id,
        Guid SubdomainId,
        string From,
        string To,
        string Subject,
        string Body,
        DateTimeOffset ReceivedAt,
        Guid? CampaignId = null,
        string? CampaignValue = null,
        bool IsCatchAll = false);
}
