using Spamma.App.Client.Infrastructure.Constants;

namespace Spamma.App.Client.Infrastructure.Contracts.Services;

public class Notification
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Message { get; init; } = string.Empty;

    public NotificationType Type { get; init; }

    public DateTime CreatedAt { get; init; } = DateTime.Now;

    public int DurationMs { get; init; } = 5000; // 5 seconds default

    public bool IsVisible { get; set; } = true;

    public bool AutoHide { get; init; } = true;
}