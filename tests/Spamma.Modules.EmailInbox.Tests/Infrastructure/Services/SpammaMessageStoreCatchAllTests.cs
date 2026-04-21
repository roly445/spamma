using System.Buffers;
using System.Net;
using FluentAssertions;
using MaybeMonad;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Moq;
using SmtpServer;
using SmtpServer.Protocol;
using SmtpServer.Storage;
using Spamma.Modules.Common.Caching;
using Spamma.Modules.EmailInbox.Infrastructure.Constants;
using Spamma.Modules.EmailInbox.Infrastructure.Services;
using Spamma.Modules.EmailInbox.Infrastructure.Services.BackgroundJobs;
using Spamma.Modules.EmailInbox.Infrastructure.Settings;
using Xunit;

namespace Spamma.Modules.EmailInbox.Tests.Infrastructure.Services;

public class SpammaMessageStoreCatchAllTests
{
    [Fact]
    public async Task SaveAsync_StrictPort_WithCatchAllEnabled_StillValidatesDomain()
    {
        var subdomainCacheMock = new Mock<ISubdomainCache>(MockBehavior.Strict);
        var chaosAddressCacheMock = new Mock<IChaosAddressCache>(MockBehavior.Strict);
        var backgroundTaskQueueMock = new Mock<IBackgroundTaskQueue>(MockBehavior.Strict);
        var pushNotificationManager = new PushNotificationManager();

        subdomainCacheMock
            .Setup(x => x.GetSubdomainAsync("unknown.com", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<ISubdomainCache.CachedSubdomain>.Nothing);

        var settings = new EmailInboxSettings { Port = 1025, CatchAllPort = 1026, CatchAllPortEnabled = true };

        var serviceProvider = BuildServiceProvider(subdomainCacheMock.Object, chaosAddressCacheMock.Object, backgroundTaskQueueMock.Object, pushNotificationManager);

        var sessionContextMock = new Mock<ISessionContext>();
        sessionContextMock.Setup(x => x.ServiceProvider).Returns(serviceProvider);
        sessionContextMock.Setup(x => x.EndpointDefinition).Returns(MakeEndpoint(1025));

        var store = new SpammaMessageStore(pushNotificationManager, Options.Create(settings));
        var buffer = MakeBuffer(new MimeMessage { Subject = "S", From = { new MailboxAddress("s", "s@t.com") }, To = { new MailboxAddress("r", "r@unknown.com") } });

        var result = await store.SaveAsync(sessionContextMock.Object, Mock.Of<IMessageTransaction>(), buffer, CancellationToken.None);

        result.Should().Be(SmtpResponse.MailboxNameNotAllowed);
        backgroundTaskQueueMock.Verify(x => x.QueueBackgroundWorkItem(It.IsAny<IBaseEmailCaptureJob>()), Times.Never);
    }

    [Fact]
    public async Task SaveAsync_CatchAllPortEnabled_False_FallsThroughToDomainValidation()
    {
        var subdomainCacheMock = new Mock<ISubdomainCache>(MockBehavior.Strict);
        var chaosAddressCacheMock = new Mock<IChaosAddressCache>(MockBehavior.Strict);
        var backgroundTaskQueueMock = new Mock<IBackgroundTaskQueue>(MockBehavior.Strict);
        var pushNotificationManager = new PushNotificationManager();

        subdomainCacheMock
            .Setup(x => x.GetSubdomainAsync("unknown.com", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<ISubdomainCache.CachedSubdomain>.Nothing);

        var settings = new EmailInboxSettings { Port = 1025, CatchAllPort = 1026, CatchAllPortEnabled = false };

        var serviceProvider = BuildServiceProvider(subdomainCacheMock.Object, chaosAddressCacheMock.Object, backgroundTaskQueueMock.Object, pushNotificationManager);

        var sessionContextMock = new Mock<ISessionContext>();
        sessionContextMock.Setup(x => x.ServiceProvider).Returns(serviceProvider);
        sessionContextMock.Setup(x => x.EndpointDefinition).Returns(MakeEndpoint(1026));

        var store = new SpammaMessageStore(pushNotificationManager, Options.Create(settings));
        var buffer = MakeBuffer(new MimeMessage { Subject = "S", From = { new MailboxAddress("s", "s@t.com") }, To = { new MailboxAddress("r", "r@unknown.com") } });

        var result = await store.SaveAsync(sessionContextMock.Object, Mock.Of<IMessageTransaction>(), buffer, CancellationToken.None);

        result.Should().Be(SmtpResponse.MailboxNameNotAllowed);
    }

    [Fact]
    public async Task SaveAsync_CatchAllPort_DomainIdAndSubdomainIdAreDifferent()
    {
        var subdomainCacheMock = new Mock<ISubdomainCache>(MockBehavior.Strict);
        var chaosAddressCacheMock = new Mock<IChaosAddressCache>(MockBehavior.Strict);
        var backgroundTaskQueueMock = new Mock<IBackgroundTaskQueue>(MockBehavior.Strict);
        var pushNotificationManager = new PushNotificationManager();

        StandardEmailCaptureJob? capturedJob = null;
        backgroundTaskQueueMock
            .Setup(x => x.QueueBackgroundWorkItem(It.IsAny<StandardEmailCaptureJob>()))
            .Callback<IBaseEmailCaptureJob>(job => capturedJob = job as StandardEmailCaptureJob);

        var settings = new EmailInboxSettings { Port = 1025, CatchAllPort = 1026, CatchAllPortEnabled = true };
        var serviceProvider = BuildServiceProvider(subdomainCacheMock.Object, chaosAddressCacheMock.Object, backgroundTaskQueueMock.Object, pushNotificationManager);

        var sessionContextMock = new Mock<ISessionContext>();
        sessionContextMock.Setup(x => x.ServiceProvider).Returns(serviceProvider);
        sessionContextMock.Setup(x => x.EndpointDefinition).Returns(MakeEndpoint(1026));

        var store = new SpammaMessageStore(pushNotificationManager, Options.Create(settings));
        var buffer = MakeBuffer(new MimeMessage { Subject = "S", From = { new MailboxAddress("s", "s@t.com") }, To = { new MailboxAddress("r", "r@any.xyz") } });

        await store.SaveAsync(sessionContextMock.Object, Mock.Of<IMessageTransaction>(), buffer, CancellationToken.None);

        capturedJob.Should().NotBeNull();
        capturedJob!.DomainId.Should().Be(CatchAllConstants.DomainId);
        capturedJob.SubdomainId.Should().Be(CatchAllConstants.SubdomainId);
        capturedJob.DomainId.Should().NotBe(Guid.Empty);
        capturedJob.DomainId.Should().NotBe(capturedJob.SubdomainId);
    }

    private static IServiceProvider BuildServiceProvider(
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

    private static IEndpointDefinition MakeEndpoint(int port)
    {
        var mock = new Mock<IEndpointDefinition>();
        mock.Setup(x => x.Endpoint).Returns(new IPEndPoint(IPAddress.Loopback, port));
        return mock.Object;
    }

    private static ReadOnlySequence<byte> MakeBuffer(MimeMessage message)
    {
        using var ms = new MemoryStream();
        message.WriteTo(ms);
        return new ReadOnlySequence<byte>(ms.ToArray());
    }
}
