using FluentAssertions;
using Spamma.Modules.EmailInbox.Infrastructure.Services.BackgroundJobs;

namespace Spamma.Modules.EmailInbox.Tests.Infrastructure.Services;

public class BackgroundTaskQueueTests
{
    [Fact]
    public void QueueBackgroundWorkItem_WithNullWorkItem_ThrowsArgumentNullException()
    {
        // Arrange
        var queue = new BackgroundTaskQueue();

        // Act
        var act = () => queue.QueueBackgroundWorkItem(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("workItem");
    }

    [Fact]
    public async Task QueueBackgroundWorkItem_WithValidWorkItem_EnqueuesSuccessfully()
    {
        // Arrange
        var queue = new BackgroundTaskQueue();
        var workItem = new StandardEmailCaptureJob(
            new MemoryStream(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid());

        // Act
        queue.QueueBackgroundWorkItem(workItem);
        var dequeuedItem = await queue.DequeueAsync(CancellationToken.None);

        // Assert
        dequeuedItem.Should().BeSameAs(workItem);
    }

    [Fact]
    public async Task DequeueAsync_WaitsForItemToBeQueued()
    {
        // Arrange
        var queue = new BackgroundTaskQueue();
        var workItem = new StandardEmailCaptureJob(
            new MemoryStream(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid());

        // Act - Start dequeue operation before enqueueing
        var dequeueTask = Task.Run(async () => await queue.DequeueAsync(CancellationToken.None));

        // Wait a bit to ensure dequeue is waiting
        await Task.Delay(50);

        // Now enqueue the item
        queue.QueueBackgroundWorkItem(workItem);

        // Wait for dequeue to complete
        var dequeuedItem = await dequeueTask;

        // Assert
        dequeuedItem.Should().BeSameAs(workItem);
    }

    [Fact]
    public async Task DequeueAsync_WithCancellationToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var queue = new BackgroundTaskQueue();
        using var cts = new CancellationTokenSource();

        // Act
        var dequeueTask = queue.DequeueAsync(cts.Token);
        await cts.CancelAsync();

        // Assert
        await dequeueTask.Invoking(async t => await t)
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task QueueBackgroundWorkItem_MultipleItems_MaintainsFifoOrder()
    {
        // Arrange
        var queue = new BackgroundTaskQueue();
        var workItem1 = new StandardEmailCaptureJob(
            new MemoryStream(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid());
        var workItem2 = new ChaosEmailCaptureJob(
            new MemoryStream(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid());
        var workItem3 = new CampaignCaptureJob(
            new MemoryStream(),
            Guid.NewGuid(),
            Guid.NewGuid());

        // Act
        queue.QueueBackgroundWorkItem(workItem1);
        queue.QueueBackgroundWorkItem(workItem2);
        queue.QueueBackgroundWorkItem(workItem3);

        var dequeued1 = await queue.DequeueAsync(CancellationToken.None);
        var dequeued2 = await queue.DequeueAsync(CancellationToken.None);
        var dequeued3 = await queue.DequeueAsync(CancellationToken.None);

        // Assert
        dequeued1.Should().BeSameAs(workItem1);
        dequeued2.Should().BeSameAs(workItem2);
        dequeued3.Should().BeSameAs(workItem3);
    }

    [Fact]
    public async Task QueueBackgroundWorkItem_ConcurrentEnqueues_AllItemsDequeued()
    {
        // Arrange
        var queue = new BackgroundTaskQueue();
        const int itemCount = 100;
        var workItems = Enumerable.Range(0, itemCount)
            .Select(_ => new StandardEmailCaptureJob(
                new MemoryStream(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid()))
            .ToList();

        // Act - Enqueue concurrently
        var enqueueTasks = workItems.Select(item =>
            Task.Run(() => queue.QueueBackgroundWorkItem(item))).ToArray();

        await Task.WhenAll(enqueueTasks);

        // Dequeue all items
        var dequeuedItems = new List<IBaseEmailCaptureJob>();
        for (var i = 0; i < itemCount; i++)
        {
            dequeuedItems.Add(await queue.DequeueAsync(CancellationToken.None));
        }

        // Assert - All items should be dequeued (order may vary due to concurrency)
        dequeuedItems.Should().HaveCount(itemCount);
        dequeuedItems.Should().OnlyHaveUniqueItems();
        var dequeuedStandardJobs = dequeuedItems.Cast<StandardEmailCaptureJob>().ToList();
        var expectedIds = workItems.Select(x => (x.DomainId, x.SubdomainId)).ToList();
        var actualIds = dequeuedStandardJobs.Select(x => (x.DomainId, x.SubdomainId)).ToList();
        actualIds.Should().BeEquivalentTo(expectedIds);
    }

    [Fact]
    public async Task QueueBackgroundWorkItem_ConcurrentDequeues_AllItemsReceivedOnce()
    {
        // Arrange
        var queue = new BackgroundTaskQueue();
        const int itemCount = 50;
        var workItems = Enumerable.Range(0, itemCount)
            .Select(_ => new StandardEmailCaptureJob(
                new MemoryStream(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid()))
            .ToList();

        // Enqueue all items
        foreach (var item in workItems)
        {
            queue.QueueBackgroundWorkItem(item);
        }

        // Act - Dequeue concurrently
        var dequeueTasks = Enumerable.Range(0, itemCount)
            .Select(_ => Task.Run(async () => await queue.DequeueAsync(CancellationToken.None)))
            .ToArray();

        var dequeuedItems = await Task.WhenAll(dequeueTasks);

        // Assert - Each item should be dequeued exactly once
        dequeuedItems.Should().HaveCount(itemCount);
        dequeuedItems.Should().OnlyHaveUniqueItems();
        var dequeuedStandardJobs = dequeuedItems.Cast<StandardEmailCaptureJob>().ToList();
        var expectedIds = workItems.Select(x => (x.DomainId, x.SubdomainId)).ToList();
        var actualIds = dequeuedStandardJobs.Select(x => (x.DomainId, x.SubdomainId)).ToList();
        actualIds.Should().BeEquivalentTo(expectedIds);
    }
}
