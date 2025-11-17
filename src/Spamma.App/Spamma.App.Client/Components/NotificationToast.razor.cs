using Microsoft.AspNetCore.Components;
using Spamma.App.Client.Infrastructure.Constants;
using Spamma.App.Client.Infrastructure.Contracts.Services;

namespace Spamma.App.Client.Components;

/// <summary>
/// Code-behind for the notification toast component.
/// </summary>
public partial class NotificationToast : ComponentBase
{
    [Parameter]
    public Notification Notification { get; set; } = null!;

    [Parameter]
    public EventCallback<Guid> OnRemove { get; set; }

    [Parameter]
    public bool ShowProgressBar { get; set; } = true;

    private bool IsVisible => this.Notification.IsVisible;

    private void HandleClose()
    {
        this.OnRemove.InvokeAsync(this.Notification.Id);
    }

    private void HandleClick()
    {
        if (this.Notification.Type == NotificationType.Error)
        {
            return;
        }

        this.HandleClose();
    }

    private string GetTypeClass() => this.Notification.Type switch
    {
        NotificationType.Success => "notification-success",
        NotificationType.Info => "notification-info",
        NotificationType.Warning => "notification-warning",
        NotificationType.Error => "notification-error",
        _ => "notification-info",
    };

    private MarkupString GetIcon() => this.Notification.Type switch
    {
        NotificationType.Success => new MarkupString("<span>✓</span>"),
        NotificationType.Info => new MarkupString("<span>ℹ</span>"),
        NotificationType.Warning => new MarkupString("<span>⚠</span>"),
        NotificationType.Error => new MarkupString("<span>✕</span>"),
        _ => new MarkupString("<span>ℹ</span>"),
    };
}