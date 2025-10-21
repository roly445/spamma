using System.Collections.Concurrent;
using Spamma.App.Client.Infrastructure.Constants;
using Spamma.App.Client.Infrastructure.Contracts.Services;

namespace Spamma.App.Client.Infrastructure.Services;

public sealed class NotificationService : INotificationService, IDisposable
{
    private readonly ConcurrentDictionary<Guid, Notification> _notifications = new();
    private readonly List<Notification> _visibleNotifications = new();
    private readonly object _listLock = new();
    private readonly Timer _cleanupTimer;

    public NotificationService()
    {
        // Clean up expired notifications every second
        this._cleanupTimer = new Timer(this.CleanupExpiredNotifications, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }

    public event Action? OnChange;

    public IReadOnlyList<Notification> Notifications
    {
        get
        {
            lock (this._listLock)
            {
                return this._visibleNotifications.AsReadOnly();
            }
        }
    }

    public void ShowSuccess(string message, int? durationMs = null)
    {
        this.Show(message, NotificationType.Success, durationMs);
    }

    public void ShowInfo(string message, int? durationMs = null)
    {
        this.Show(message, NotificationType.Info, durationMs);
    }

    public void ShowWarning(string message, int? durationMs = null)
    {
        this.Show(message, NotificationType.Warning, durationMs);
    }

    public void ShowError(string message, int? durationMs = null)
    {
        this.Show(message, NotificationType.Error, durationMs ?? 10000); // Errors stay longer by default
    }

    public void Show(string message, NotificationType type, int? durationMs = null, bool autoHide = true)
    {
        var notification = new Notification
        {
            Message = message,
            Type = type,
            DurationMs = durationMs ?? 5000,
            AutoHide = autoHide,
        };

        this._notifications[notification.Id] = notification;
        lock (this._listLock)
        {
            this._visibleNotifications.Add(notification);
            this._visibleNotifications.Sort((a, b) => b.CreatedAt.CompareTo(a.CreatedAt));
        }

        this.OnChange?.Invoke();
    }

    public void Remove(Guid notificationId)
    {
        if (this._notifications.TryGetValue(notificationId, out var notification))
        {
            notification.IsVisible = false;
            this.OnChange?.Invoke();

            // Remove from dictionary after a short delay to allow for animations
            _ = Task.Delay(300).ContinueWith(_ => this._notifications.TryRemove(notificationId, out var _));
        }
    }

    public void Clear()
    {
        foreach (var notification in this._notifications.Values)
        {
            notification.IsVisible = false;
        }

        this.OnChange?.Invoke();

        // Clear dictionary after animation delay
        _ = Task.Delay(300).ContinueWith(_ => this._notifications.Clear());
    }

    public void Dispose()
    {
        this._cleanupTimer.Dispose();
    }

    private void CleanupExpiredNotifications(object? state)
    {
        var now = DateTime.Now;
        var expiredNotifications = this._notifications.Values
            .Where(n => n is { IsVisible: true, AutoHide: true } &&
                        (now - n.CreatedAt).TotalMilliseconds > n.DurationMs)
            .ToList();

        if (expiredNotifications.Any())
        {
            foreach (var notification in expiredNotifications)
            {
                notification.IsVisible = false;
            }

            this.OnChange?.Invoke();

            // Remove from dictionary after animation delay
            _ = Task.Delay(300).ContinueWith(_ =>
            {
                foreach (var notification in expiredNotifications)
                {
                    this._notifications.TryRemove(notification.Id, out var _);
                }
            });
        }
    }
}