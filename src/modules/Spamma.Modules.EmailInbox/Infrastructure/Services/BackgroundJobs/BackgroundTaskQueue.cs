using System.Collections.Concurrent;

namespace Spamma.Modules.EmailInbox.Infrastructure.Services.BackgroundJobs;

public class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly ConcurrentQueue<IBaseEmailCaptureJob> _workItems = new();
    private readonly SemaphoreSlim _signal = new(0);

    public void QueueBackgroundWorkItem(IBaseEmailCaptureJob workItem)
    {
        if (workItem == null)
        {
            throw new ArgumentNullException(nameof(workItem));
        }

        this._workItems.Enqueue(workItem);
        this._signal.Release();
    }

    public async Task<IBaseEmailCaptureJob> DequeueAsync(CancellationToken cancellationToken)
    {
        await this._signal.WaitAsync(cancellationToken);
        this._workItems.TryDequeue(out var workItem);

        return workItem!;
    }
}