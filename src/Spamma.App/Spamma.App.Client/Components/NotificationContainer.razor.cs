using Microsoft.AspNetCore.Components;
using Spamma.App.Client.Infrastructure.Contracts.Services;

namespace Spamma.App.Client.Components;

/// <summary>
/// Backing code for the <see cref="NotificationContainer"/> component.
/// </summary>
public partial class NotificationContainer(INotificationService notificationService) : ComponentBase, IDisposable
{
    private List<Notification> notifications = new();
    private bool _disposed;

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (this._disposed)
        {
            return;
        }

        if (disposing)
        {
            notificationService.OnChange -= this.HandleNotificationChange;
        }

        this._disposed = true;
    }

    protected override void OnInitialized()
    {
        notificationService.OnChange += this.HandleNotificationChange;
        this.UpdateNotifications();
    }

    private void HandleNotificationChange()
    {
        this.UpdateNotifications();
        this.InvokeAsync(this.StateHasChanged);
    }

    private void UpdateNotifications()
    {
        this.notifications = notificationService.Notifications.ToList();
    }

    private void HandleRemove(Guid notificationId)
    {
        notificationService.Remove(notificationId);
    }
}