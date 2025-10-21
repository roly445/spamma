using Spamma.App.Client.Infrastructure.Constants;

namespace Spamma.App.Client.Infrastructure.Contracts.Services;

public interface INotificationService
{
    event Action? OnChange;

    IReadOnlyList<Notification> Notifications { get; }

    void ShowSuccess(string message, int? durationMs = null);

    void ShowInfo(string message, int? durationMs = null);

    void ShowWarning(string message, int? durationMs = null);

    void ShowError(string message, int? durationMs = null);

    void Show(string message, NotificationType type, int? durationMs = null, bool autoHide = true);

    void Remove(Guid notificationId);

    void Clear();
}