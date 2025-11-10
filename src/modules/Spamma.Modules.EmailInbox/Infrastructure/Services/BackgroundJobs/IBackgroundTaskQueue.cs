namespace Spamma.Modules.EmailInbox.Infrastructure.Services.BackgroundJobs;

public interface IBackgroundTaskQueue
{
    void QueueBackgroundWorkItem(IBaseEmailCaptureJob workItem);

    Task<IBaseEmailCaptureJob> DequeueAsync(CancellationToken cancellationToken);
}