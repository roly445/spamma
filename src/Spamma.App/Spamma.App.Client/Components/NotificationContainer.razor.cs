using Microsoft.AspNetCore.Components;
using Spamma.App.Client.Infrastructure.Contracts.Services;

namespace Spamma.App.Client.Components;

/// <summary>
/// Backing code for the <see cref="NotificationContainer"/> component.
/// </summary>
public partial class NotificationContainer(INotificationService notificationService) : ComponentBase, IDisposable
{
    private List<Notification> notifications = new();

    public void Dispose()
    {
        notificationService.OnChange -= this.HandleNotificationChange;
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