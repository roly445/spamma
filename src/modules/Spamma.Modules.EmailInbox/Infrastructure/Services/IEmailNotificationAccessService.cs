namespace Spamma.Modules.EmailInbox.Infrastructure.Services;

public interface IEmailNotificationAccessService
{
    Task<bool> CanAccessNotificationAsync(Guid userId, PushNotificationManager.EmailDetails email, CancellationToken cancellationToken);

    Task<bool> CanAccessEmailAsync(Guid userId, Guid emailId, CancellationToken cancellationToken);
}
