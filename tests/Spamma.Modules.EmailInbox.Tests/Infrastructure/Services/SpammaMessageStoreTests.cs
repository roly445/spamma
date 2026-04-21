using System.Buffers;
using FluentAssertions;
using MaybeMonad;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MimeKit;
using Moq;
using SmtpServer;
using SmtpServer.Protocol;
using SmtpServer.Storage;
using Spamma.Modules.Common.Caching;
using Spamma.Modules.Common.Client;
using Spamma.Modules.EmailInbox.Infrastructure.Services;
using Spamma.Modules.EmailInbox.Infrastructure.Services.BackgroundJobs;
using Xunit;

namespace Spamma.Modules.EmailInbox.Tests.Infrastructure.Services;

/// <summary>
/// Tests for SpammaMessageStore - tests SMTP message reception flow including:
/// subdomain/chaos address cache lookups, background job queueing, and push notifications.
/// </summary>
public class SpammaMessageStoreTests
{
    [Fact]
    public async Task SaveAsync_ValidEmailWithActiveSubdomain_QueuesStandardJobAndReturnsOk()
    {
        // Arrange
        var subdomainCacheMock = new Mock<ISubdomainCache>(MockBehavior.Strict);
        var chaosAddressCacheMock = new Mock<IChaosAddressCache>(MockBehavior.Strict);
        var backgroundTaskQueueMock = new Mock<IBackgroundTaskQueue>(MockBehavior.Strict);
        var pushNotificationManagerMock = new Mock<PushNotificationManager>();

        var subdomainId = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var cachedSubdomain = new ISubdomainCache.CachedSubdomain(subdomainId, domainId);

        subdomainCacheMock
            .Setup(x => x.GetSubdomainAsync("example.com", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(cachedSubdomain));

        chaosAddressCacheMock
            .Setup(x => x.GetChaosAddressAsync(subdomainId, "recipient", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<IChaosAddressCache.CachedChaosAddress>.Nothing);

        StandardEmailCaptureJob? capturedJob = null;
        backgroundTaskQueueMock
            .Setup(x => x.QueueBackgroundWorkItem(It.IsAny<StandardEmailCaptureJob>()))
            .Callback<IBaseEmailCaptureJob>(job => capturedJob = job as StandardEmailCaptureJob);

        pushNotificationManagerMock
            .Setup(x => x.NotifyEmailAsync(It.IsAny<PushNotificationManager.EmailDetails>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var serviceProvider = CreateServiceProvider(
            subdomainCacheMock.Object,
            chaosAddressCacheMock.Object,
            backgroundTaskQueueMock.Object,
            pushNotificationManagerMock.Object);

        var sessionContextMock = new Mock<ISessionContext>();
        sessionContextMock.Setup(x => x.ServiceProvider).Returns(serviceProvider);

        var store = new SpammaMessageStore(pushNotificationManagerMock.Object);

        var mimeMessage = new MimeMessage
        {
            Subject = "Test Email",
            From = { new MailboxAddress("sender", "sender@test.com") },
            To = { new MailboxAddress("recipient", "recipient@example.com") },
        };

        var buffer = CreateBuffer(mimeMessage);

        // Act
        var result = await store.SaveAsync(
            sessionContextMock.Object,
            Mock.Of<IMessageTransaction>(),
            buffer,
            CancellationToken.None);

        // Assert
        result.Should().Be(SmtpResponse.Ok);
        backgroundTaskQueueMock.Verify(x => x.QueueBackgroundWorkItem(It.IsAny<StandardEmailCaptureJob>()), Times.Once);
        capturedJob.Should().NotBeNull();
        capturedJob!.DomainId.Should().Be(domainId);
        capturedJob.SubdomainId.Should().Be(subdomainId);
        pushNotificationManagerMock.Verify(
            x => x.NotifyEmailAsync(It.IsAny<PushNotificationManager.EmailDetails>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SaveAsync_NoMatchingSubdomain_ReturnsMailboxNameNotAllowed()
    {
        // Arrange
        var subdomainCacheMock = new Mock<ISubdomainCache>(MockBehavior.Strict);
        var chaosAddressCacheMock = new Mock<IChaosAddressCache>(MockBehavior.Strict);
        var backgroundTaskQueueMock = new Mock<IBackgroundTaskQueue>(MockBehavior.Strict);
        var pushNotificationManagerMock = new Mock<PushNotificationManager>();

        subdomainCacheMock
            .Setup(x => x.GetSubdomainAsync("unknown.com", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<ISubdomainCache.CachedSubdomain>.Nothing);

        var serviceProvider = CreateServiceProvider(
            subdomainCacheMock.Object,
            chaosAddressCacheMock.Object,
            backgroundTaskQueueMock.Object,
            pushNotificationManagerMock.Object);

        var sessionContextMock = new Mock<ISessionContext>();
        sessionContextMock.Setup(x => x.ServiceProvider).Returns(serviceProvider);

        var store = new SpammaMessageStore(pushNotificationManagerMock.Object);

        var mimeMessage = new MimeMessage
        {
            Subject = "Test Email",
            From = { new MailboxAddress("sender", "sender@test.com") },
            To = { new MailboxAddress("recipient", "recipient@unknown.com") },
        };

        var buffer = CreateBuffer(mimeMessage);

        // Act
        var result = await store.SaveAsync(
            sessionContextMock.Object,
            Mock.Of<IMessageTransaction>(),
            buffer,
            CancellationToken.None);

        // Assert
        result.Should().Be(SmtpResponse.MailboxNameNotAllowed);
        backgroundTaskQueueMock.Verify(x => x.QueueBackgroundWorkItem(It.IsAny<IBaseEmailCaptureJob>()), Times.Never);
        pushNotificationManagerMock.Verify(
            x => x.NotifyEmailAsync(It.IsAny<PushNotificationManager.EmailDetails>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SaveAsync_WithChaosAddressMatch_QueuesChaosJobAndReturnsConfiguredSmtpCode()
    {
        // Arrange
        var subdomainCacheMock = new Mock<ISubdomainCache>(MockBehavior.Strict);
        var chaosAddressCacheMock = new Mock<IChaosAddressCache>(MockBehavior.Strict);
        var backgroundTaskQueueMock = new Mock<IBackgroundTaskQueue>(MockBehavior.Strict);
        var pushNotificationManagerMock = new Mock<PushNotificationManager>();

        var subdomainId = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var chaosAddressId = Guid.NewGuid();
        var cachedSubdomain = new ISubdomainCache.CachedSubdomain(subdomainId, domainId);
        var cachedChaosAddress = new IChaosAddressCache.CachedChaosAddress(
            chaosAddressId,
            domainId,
            subdomainId,
            SmtpResponseCode.RequestedActionAborted);

        subdomainCacheMock
            .Setup(x => x.GetSubdomainAsync("example.com", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(cachedSubdomain));

        chaosAddressCacheMock
            .Setup(x => x.GetChaosAddressAsync(subdomainId, "chaos", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(cachedChaosAddress));

        ChaosEmailCaptureJob? capturedJob = null;
        backgroundTaskQueueMock
            .Setup(x => x.QueueBackgroundWorkItem(It.IsAny<ChaosEmailCaptureJob>()))
            .Callback<IBaseEmailCaptureJob>(job => capturedJob = job as ChaosEmailCaptureJob);

        pushNotificationManagerMock
            .Setup(x => x.NotifyEmailAsync(It.IsAny<PushNotificationManager.EmailDetails>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var serviceProvider = CreateServiceProvider(
            subdomainCacheMock.Object,
            chaosAddressCacheMock.Object,
            backgroundTaskQueueMock.Object,
            pushNotificationManagerMock.Object);

        var sessionContextMock = new Mock<ISessionContext>();
        sessionContextMock.Setup(x => x.ServiceProvider).Returns(serviceProvider);

        var store = new SpammaMessageStore(pushNotificationManagerMock.Object);

        var mimeMessage = new MimeMessage
        {
            Subject = "Test Email",
            From = { new MailboxAddress("sender", "sender@test.com") },
            To = { new MailboxAddress("chaos", "chaos@example.com") },
        };

        var buffer = CreateBuffer(mimeMessage);

        // Act
        var result = await store.SaveAsync(
            sessionContextMock.Object,
            Mock.Of<IMessageTransaction>(),
            buffer,
            CancellationToken.None);

        // Assert
        result.ReplyCode.Should().Be((SmtpReplyCode)451);
        backgroundTaskQueueMock.Verify(x => x.QueueBackgroundWorkItem(It.IsAny<ChaosEmailCaptureJob>()), Times.Once);
        capturedJob.Should().NotBeNull();
        capturedJob!.ChaosAddressId.Should().Be(chaosAddressId);
        capturedJob.DomainId.Should().Be(domainId);
        capturedJob.SubdomainId.Should().Be(subdomainId);
    }

    [Fact]
    public async Task SaveAsync_MultipleRecipients_UsesFirstMatchingSubdomain()
    {
        // Arrange
        var subdomainCacheMock = new Mock<ISubdomainCache>(MockBehavior.Strict);
        var chaosAddressCacheMock = new Mock<IChaosAddressCache>(MockBehavior.Strict);
        var backgroundTaskQueueMock = new Mock<IBackgroundTaskQueue>(MockBehavior.Strict);
        var pushNotificationManagerMock = new Mock<PushNotificationManager>();

        var subdomainId = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var cachedSubdomain = new ISubdomainCache.CachedSubdomain(subdomainId, domainId);

        // First recipient: no match
        subdomainCacheMock
            .Setup(x => x.GetSubdomainAsync("unknown.com", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<ISubdomainCache.CachedSubdomain>.Nothing);

        // Second recipient: match
        subdomainCacheMock
            .Setup(x => x.GetSubdomainAsync("example.com", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(cachedSubdomain));

        chaosAddressCacheMock
            .Setup(x => x.GetChaosAddressAsync(subdomainId, "valid", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<IChaosAddressCache.CachedChaosAddress>.Nothing);

        backgroundTaskQueueMock
            .Setup(x => x.QueueBackgroundWorkItem(It.IsAny<StandardEmailCaptureJob>()));

        pushNotificationManagerMock
            .Setup(x => x.NotifyEmailAsync(It.IsAny<PushNotificationManager.EmailDetails>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var serviceProvider = CreateServiceProvider(
            subdomainCacheMock.Object,
            chaosAddressCacheMock.Object,
            backgroundTaskQueueMock.Object,
            pushNotificationManagerMock.Object);

        var sessionContextMock = new Mock<ISessionContext>();
        sessionContextMock.Setup(x => x.ServiceProvider).Returns(serviceProvider);

        var store = new SpammaMessageStore(pushNotificationManagerMock.Object);

        var mimeMessage = new MimeMessage
        {
            Subject = "Test Email",
            From = { new MailboxAddress("sender", "sender@test.com") },
            To =
            {
                new MailboxAddress("bad", "bad@unknown.com"),
                new MailboxAddress("valid", "valid@example.com"),
            },
        };

        var buffer = CreateBuffer(mimeMessage);

        // Act
        var result = await store.SaveAsync(
            sessionContextMock.Object,
            Mock.Of<IMessageTransaction>(),
            buffer,
            CancellationToken.None);

        // Assert
        result.Should().Be(SmtpResponse.Ok);
        backgroundTaskQueueMock.Verify(x => x.QueueBackgroundWorkItem(It.IsAny<StandardEmailCaptureJob>()), Times.Once);
    }

    private static IServiceProvider CreateServiceProvider(
        ISubdomainCache subdomainCache,
        IChaosAddressCache chaosAddressCache,
        IBackgroundTaskQueue backgroundTaskQueue,
        PushNotificationManager pushNotificationManager)
    {
        var services = new ServiceCollection();
        services.AddSingleton(subdomainCache);
        services.AddSingleton(chaosAddressCache);
        services.AddSingleton(backgroundTaskQueue);
        services.AddSingleton(pushNotificationManager);
        services.AddSingleton<ILogger<SpammaMessageStore>>(new Mock<ILogger<SpammaMessageStore>>().Object);
        return services.BuildServiceProvider();
    }

    private static ReadOnlySequence<byte> CreateBuffer(MimeMessage message)
    {
        using var memoryStream = new MemoryStream();
        message.WriteTo(memoryStream);
        return new ReadOnlySequence<byte>(memoryStream.ToArray());
    }
}
