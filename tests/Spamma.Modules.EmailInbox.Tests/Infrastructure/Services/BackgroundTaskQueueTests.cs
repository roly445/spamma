using System.Text;
using System.Text.Json;
using DotNetCore.CAP;
using FluentAssertions;
using Moq;
using Spamma.Modules.EmailInbox.Infrastructure.Services.BackgroundJobs;

namespace Spamma.Modules.EmailInbox.Tests.Infrastructure.Services;

public class BackgroundTaskQueueTests
{
    [Fact]
    public void QueueBackgroundWorkItem_WithNullWorkItem_ThrowsArgumentNullException()
    {
        var queue = new BackgroundTaskQueue(Mock.Of<ICapPublisher>());

        var act = () => queue.QueueBackgroundWorkItem(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("workItem");
    }

    [Fact]
    public void QueueBackgroundWorkItem_PublishesRecoverableMimePayload()
    {
        var mime = Encoding.UTF8.GetBytes("From: sender@test.com\r\nTo: recipient@test.com\r\nSubject: saved\r\n\r\nbody");
        var messageId = Guid.NewGuid();
        EmailCaptureEnvelope? published = null;
        var publisher = new Mock<ICapPublisher>();
        publisher.Setup(x => x.Publish(BackgroundTaskQueue.CaptureTopic, It.IsAny<EmailCaptureEnvelope>(), (string?)null))
            .Callback<string, EmailCaptureEnvelope, string?>((_, payload, _) => published = payload);
        var queue = new BackgroundTaskQueue(publisher.Object);

        queue.QueueBackgroundWorkItem(new StandardEmailCaptureJob(
            new MemoryStream(mime), Guid.NewGuid(), Guid.NewGuid(), messageId));

        published.Should().NotBeNull();
        published!.MessageId.Should().Be(messageId);
        published.MimeContent.Should().Equal(mime);
        var restartedPayload = JsonSerializer.Deserialize<EmailCaptureEnvelope>(JsonSerializer.Serialize(published));
        restartedPayload.Should().NotBeNull();
        using var recoveredJob = restartedPayload!.ToJob().MimeStream;
        using var recoveredContent = new MemoryStream();
        recoveredJob.CopyTo(recoveredContent);
        recoveredContent.ToArray().Should().Equal(mime);
    }

    [Fact]
    public void QueueBackgroundWorkItem_WhenDurablePublishFails_PropagatesFailure()
    {
        var publisher = new Mock<ICapPublisher>();
        publisher.Setup(x => x.Publish(BackgroundTaskQueue.CaptureTopic, It.IsAny<EmailCaptureEnvelope>(), (string?)null))
            .Throws(new IOException("PostgreSQL unavailable"));
        var queue = new BackgroundTaskQueue(publisher.Object);
        var job = new StandardEmailCaptureJob(new MemoryStream([1, 2, 3]), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        var act = () => queue.QueueBackgroundWorkItem(job);

        act.Should().Throw<IOException>();
    }
}
