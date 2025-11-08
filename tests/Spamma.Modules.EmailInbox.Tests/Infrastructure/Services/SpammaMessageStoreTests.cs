using System.Buffers;
using System.Threading.Channels;
using BluQube.Commands;
using FluentAssertions;
using MaybeMonad;
using Microsoft.Extensions.DependencyInjection;
using MimeKit;
using Moq;
using ResultMonad;
using SmtpServer.Protocol;
using Spamma.Modules.Common.Client;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.DomainManagement.Client.Application.Queries;
using Spamma.Modules.DomainManagement.Client.Contracts;
using Spamma.Modules.DomainManagement.Client.Infrastructure.Caching;
using Spamma.Modules.EmailInbox.Client.Application.Commands;
using Spamma.Modules.EmailInbox.Infrastructure.Services;
using Spamma.Modules.EmailInbox.Infrastructure.Services.BackgroundJobs;

namespace Spamma.Modules.EmailInbox.Tests.Infrastructure.Services;

/// <summary>
/// Integration tests for SpammaMessageStore - tests SMTP message reception flow including:
/// subdomain/chaos address cache lookups, message storage, command execution, and background job queueing.
/// </summary>
public class SpammaMessageStoreTests
{
    [Fact]
    public async Task SaveAsync_ValidEmailWithActiveSubdomain_StoresMessageAndReturnsOk()
    {
        // Arrange
        var domainId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();

        var mimeMessage = new MimeMessage
        {
            Subject = "Test Email",
            Date = DateTimeOffset.UtcNow,
            From = { new MailboxAddress("sender", "sender@example.com") },
            To = { new MailboxAddress("recipient", "test@spamma.io") },
        };
        mimeMessage.Body = new TextPart("plain") { Text = "Test body" };

        var buffer = CreateBufferFromMimeMessage(mimeMessage);

        var (serviceProvider, mocks) = CreateMockServiceProvider();

        // Mock subdomain cache to return active subdomain
        mocks.SubdomainCacheMock
            .Setup(x => x.GetSubdomainAsync("spamma.io", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(new SearchSubdomainsQueryResult.SubdomainSummary(
                SubdomainId: subdomainId,
                ParentDomainId: domainId,
                SubdomainName: "spamma",
                ParentDomainName: "io",
                FullDomainName: "spamma.io",
                Status: SubdomainStatus.Active,
                CreatedAt: DateTime.UtcNow,
                ChaosMonkeyRuleCount: 0,
                ActiveCampaignCount: 0,
                Description: null)));

        // Mock chaos address cache to return no chaos address
        mocks.ChaosAddressCacheMock
            .Setup(x => x.GetChaosAddressAsync(subdomainId, "test", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<GetChaosAddressBySubdomainAndLocalPartQueryResult>.Nothing);

        // Mock message store provider
        mocks.MessageStoreProviderMock
            .Setup(x => x.StoreMessageContentAsync(It.IsAny<Guid>(), It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        // Mock commander
        mocks.CommanderMock
            .Setup(x => x.Send(It.IsAny<ReceivedEmailCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult.Succeeded());

        // Act
        var result = await SpammaMessageStore.SaveAsyncWithProvider(serviceProvider, buffer, CancellationToken.None);

        // Assert
        result.Should().Be(SmtpResponse.Ok);
        mocks.MessageStoreProviderMock.Verify(x => x.StoreMessageContentAsync(It.IsAny<Guid>(), It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        mocks.CommanderMock.Verify(
            x => x.Send(
                It.Is<ReceivedEmailCommand>(cmd =>
                    cmd.DomainId == domainId &&
                    cmd.SubdomainId == subdomainId &&
                    cmd.Subject == "Test Email"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SaveAsync_NoMatchingSubdomain_ReturnsMailboxNameNotAllowed()
    {
        // Arrange
        var mimeMessage = new MimeMessage
        {
            Subject = "Test Email",
            From = { new MailboxAddress("sender", "sender@example.com") },
            To = { new MailboxAddress("user", "user@unknown.com") },
        };
        mimeMessage.Body = new TextPart("plain") { Text = "Test" };

        var buffer = CreateBufferFromMimeMessage(mimeMessage);

        var (serviceProvider, mocks) = CreateMockServiceProvider();

        // Mock subdomain cache to return no subdomain
        mocks.SubdomainCacheMock
            .Setup(x => x.GetSubdomainAsync("unknown.com", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<SearchSubdomainsQueryResult.SubdomainSummary>.Nothing);

        // Act
        var result = await SpammaMessageStore.SaveAsyncWithProvider(serviceProvider, buffer, CancellationToken.None);

        // Assert
        result.Should().Be(SmtpResponse.MailboxNameNotAllowed);
        mocks.MessageStoreProviderMock.Verify(x => x.StoreMessageContentAsync(It.IsAny<Guid>(), It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SaveAsync_MessageStorageFailure_ReturnsTransactionFailed()
    {
        // Arrange
        var domainId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();

        var mimeMessage = new MimeMessage
        {
            Subject = "Test Email",
            From = { new MailboxAddress("sender", "sender@example.com") },
            To = { new MailboxAddress("recipient", "test@spamma.io") },
        };
        mimeMessage.Body = new TextPart("plain") { Text = "Test" };

        var buffer = CreateBufferFromMimeMessage(mimeMessage);

        var (serviceProvider, mocks) = CreateMockServiceProvider();

        // Mock subdomain cache
        mocks.SubdomainCacheMock
            .Setup(x => x.GetSubdomainAsync("spamma.io", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(new SearchSubdomainsQueryResult.SubdomainSummary(
                SubdomainId: subdomainId,
                ParentDomainId: domainId,
                SubdomainName: "spamma",
                ParentDomainName: "io",
                FullDomainName: "spamma.io",
                Status: SubdomainStatus.Active,
                CreatedAt: DateTime.UtcNow,
                ChaosMonkeyRuleCount: 0,
                ActiveCampaignCount: 0,
                Description: null)));

        // Mock chaos address cache
        mocks.ChaosAddressCacheMock
            .Setup(x => x.GetChaosAddressAsync(subdomainId, "test", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<GetChaosAddressBySubdomainAndLocalPartQueryResult>.Nothing);

        // Mock message store provider to fail
        mocks.MessageStoreProviderMock
            .Setup(x => x.StoreMessageContentAsync(It.IsAny<Guid>(), It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail());

        // Act
        var result = await SpammaMessageStore.SaveAsyncWithProvider(serviceProvider, buffer, CancellationToken.None);

        // Assert
        result.Should().Be(SmtpResponse.TransactionFailed);
        mocks.CommanderMock.Verify(x => x.Send(It.IsAny<ReceivedEmailCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SaveAsync_CommandHandlerFails_DeletesStoredContentAndReturnsTransactionFailed()
    {
        // Arrange
        var domainId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();

        var mimeMessage = new MimeMessage
        {
            Subject = "Test Email",
            From = { new MailboxAddress("sender", "sender@example.com") },
            To = { new MailboxAddress("recipient", "test@spamma.io") },
        };
        mimeMessage.Body = new TextPart("plain") { Text = "Test" };

        var buffer = CreateBufferFromMimeMessage(mimeMessage);

        var (serviceProvider, mocks) = CreateMockServiceProvider();

        // Mock subdomain cache
        mocks.SubdomainCacheMock
            .Setup(x => x.GetSubdomainAsync("spamma.io", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(new SearchSubdomainsQueryResult.SubdomainSummary(
                SubdomainId: subdomainId,
                ParentDomainId: domainId,
                SubdomainName: "spamma",
                ParentDomainName: "io",
                FullDomainName: "spamma.io",
                Status: SubdomainStatus.Active,
                CreatedAt: DateTime.UtcNow,
                ChaosMonkeyRuleCount: 0,
                ActiveCampaignCount: 0,
                Description: null)));

        // Mock chaos address cache
        mocks.ChaosAddressCacheMock
            .Setup(x => x.GetChaosAddressAsync(subdomainId, "test", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<GetChaosAddressBySubdomainAndLocalPartQueryResult>.Nothing);

        // Mock message store provider to succeed
        mocks.MessageStoreProviderMock
            .Setup(x => x.StoreMessageContentAsync(It.IsAny<Guid>(), It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        mocks.MessageStoreProviderMock
            .Setup(x => x.DeleteMessageContentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        // Mock commander to fail
        mocks.CommanderMock
            .Setup(x => x.Send(It.IsAny<ReceivedEmailCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.SavingChangesFailed, "Test failure")));

        // Act
        var result = await SpammaMessageStore.SaveAsyncWithProvider(serviceProvider, buffer, CancellationToken.None);

        // Assert
        result.Should().Be(SmtpResponse.TransactionFailed);
        mocks.MessageStoreProviderMock.Verify(x => x.DeleteMessageContentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once, "Should rollback by deleting stored content");
    }

    [Fact]
    public async Task SaveAsync_WithChaosAddressMatch_ReturnsConfiguredSmtpCode()
    {
        // Arrange
        var chaosAddressId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();
        var domainId = Guid.NewGuid();

        var mimeMessage = new MimeMessage
        {
            Subject = "Test Email",
            From = { new MailboxAddress("sender", "sender@example.com") },
            To = { new MailboxAddress("chaos", "chaos@spamma.io") },
        };
        mimeMessage.Body = new TextPart("plain") { Text = "Test" };

        var buffer = CreateBufferFromMimeMessage(mimeMessage);

        var (serviceProvider, mocks) = CreateMockServiceProvider();

        // Mock subdomain cache
        mocks.SubdomainCacheMock
            .Setup(x => x.GetSubdomainAsync("spamma.io", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(new SearchSubdomainsQueryResult.SubdomainSummary(
                SubdomainId: subdomainId,
                ParentDomainId: domainId,
                SubdomainName: "spamma",
                ParentDomainName: "io",
                FullDomainName: "spamma.io",
                Status: SubdomainStatus.Active,
                CreatedAt: DateTime.UtcNow,
                ChaosMonkeyRuleCount: 1,
                ActiveCampaignCount: 0,
                Description: null)));

        // Mock chaos address cache to return a chaos address with 550 code
        mocks.ChaosAddressCacheMock
            .Setup(x => x.GetChaosAddressAsync(subdomainId, "chaos", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(new GetChaosAddressBySubdomainAndLocalPartQueryResult(
                ChaosAddressId: chaosAddressId,
                SubdomainId: subdomainId,
                LocalPart: "chaos",
                ConfiguredSmtpCode: SmtpResponseCode.MailboxUnavailable, // 550
                Enabled: true)));

        // Act
        var result = await SpammaMessageStore.SaveAsyncWithProvider(serviceProvider, buffer, CancellationToken.None);

        // Assert
        result.ReplyCode.Should().Be((SmtpReplyCode)450);
        mocks.MessageStoreProviderMock.Verify(x => x.StoreMessageContentAsync(It.IsAny<Guid>(), It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>()), Times.Never, "Should not store message when chaos address matches");
    }

    [Fact]
    public async Task SaveAsync_WithDisabledChaosAddress_FallsBackToNormalProcessing()
    {
        // Arrange
        var chaosAddressId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();
        var domainId = Guid.NewGuid();

        var mimeMessage = new MimeMessage
        {
            Subject = "Test Email",
            From = { new MailboxAddress("sender", "sender@example.com") },
            To = { new MailboxAddress("chaos", "chaos@spamma.io") },
        };
        mimeMessage.Body = new TextPart("plain") { Text = "Test" };

        var buffer = CreateBufferFromMimeMessage(mimeMessage);

        var (serviceProvider, mocks) = CreateMockServiceProvider();

        // Mock subdomain cache
        mocks.SubdomainCacheMock
            .Setup(x => x.GetSubdomainAsync("spamma.io", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(new SearchSubdomainsQueryResult.SubdomainSummary(
                SubdomainId: subdomainId,
                ParentDomainId: domainId,
                SubdomainName: "spamma",
                ParentDomainName: "io",
                FullDomainName: "spamma.io",
                Status: SubdomainStatus.Active,
                CreatedAt: DateTime.UtcNow,
                ChaosMonkeyRuleCount: 1,
                ActiveCampaignCount: 0,
                Description: null)));

        // Mock chaos address cache to return a DISABLED chaos address
        mocks.ChaosAddressCacheMock
            .Setup(x => x.GetChaosAddressAsync(subdomainId, "chaos", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(new GetChaosAddressBySubdomainAndLocalPartQueryResult(
                ChaosAddressId: chaosAddressId,
                SubdomainId: subdomainId,
                LocalPart: "chaos",
                ConfiguredSmtpCode: SmtpResponseCode.MailboxUnavailable,
                Enabled: false))); // DISABLED

        // Mock message store and commander
        mocks.MessageStoreProviderMock
            .Setup(x => x.StoreMessageContentAsync(It.IsAny<Guid>(), It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        mocks.CommanderMock
            .Setup(x => x.Send(It.IsAny<ReceivedEmailCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult.Succeeded());

        // Act
        var result = await SpammaMessageStore.SaveAsyncWithProvider(serviceProvider, buffer, CancellationToken.None);

        // Assert
        result.Should().Be(SmtpResponse.Ok, "Disabled chaos address should fall back to normal processing");
        mocks.MessageStoreProviderMock.Verify(x => x.StoreMessageContentAsync(It.IsAny<Guid>(), It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_WithCampaignHeader_QueuesBackgroundJob()
    {
        // Arrange
        var domainId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();

        var mimeMessage = new MimeMessage
        {
            Subject = "Test Campaign Email",
            Date = DateTimeOffset.UtcNow,
            From = { new MailboxAddress("sender", "sender@example.com") },
            To = { new MailboxAddress("recipient", "test@spamma.io") },
        };
        mimeMessage.Body = new TextPart("plain") { Text = "Test" };
        mimeMessage.Headers.Add("X-Spamma-Camp", "TestCampaign123");

        var buffer = CreateBufferFromMimeMessage(mimeMessage);

        var (serviceProvider, mocks) = CreateMockServiceProvider();

        // Mock subdomain cache
        mocks.SubdomainCacheMock
            .Setup(x => x.GetSubdomainAsync("spamma.io", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(new SearchSubdomainsQueryResult.SubdomainSummary(
                SubdomainId: subdomainId,
                ParentDomainId: domainId,
                SubdomainName: "spamma",
                ParentDomainName: "io",
                FullDomainName: "spamma.io",
                Status: SubdomainStatus.Active,
                CreatedAt: DateTime.UtcNow,
                ChaosMonkeyRuleCount: 0,
                ActiveCampaignCount: 1,
                Description: null)));

        // Mock chaos address cache
        mocks.ChaosAddressCacheMock
            .Setup(x => x.GetChaosAddressAsync(subdomainId, "test", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<GetChaosAddressBySubdomainAndLocalPartQueryResult>.Nothing);

        // Mock message store and commander
        mocks.MessageStoreProviderMock
            .Setup(x => x.StoreMessageContentAsync(It.IsAny<Guid>(), It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        mocks.CommanderMock
            .Setup(x => x.Send(It.IsAny<ReceivedEmailCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult.Succeeded());

        // Act
        var result = await SpammaMessageStore.SaveAsyncWithProvider(serviceProvider, buffer, CancellationToken.None);

        // Assert
        result.Should().Be(SmtpResponse.Ok);

        // Verify campaign job was queued
        var campaignChannel = serviceProvider.GetRequiredService<Channel<CampaignCaptureJob>>();
        campaignChannel.Reader.TryRead(out var job).Should().BeTrue("Campaign job should be queued");
        job!.CampaignValue.Should().Be("testcampaign123", "Campaign value should be lowercase");
        job.DomainId.Should().Be(domainId);
        job.SubdomainId.Should().Be(subdomainId);
    }

    private static ReadOnlySequence<byte> CreateBufferFromMimeMessage(MimeMessage message)
    {
        using var stream = new MemoryStream();
        message.WriteTo(stream);
        return new ReadOnlySequence<byte>(stream.ToArray());
    }

    private static (IServiceProvider ServiceProvider, MockServices Mocks) CreateMockServiceProvider()
    {
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging();

        // Create mocks
        var messageStoreProviderMock = new Mock<IMessageStoreProvider>(MockBehavior.Strict);
        var commanderMock = new Mock<ICommander>(MockBehavior.Strict);
        var subdomainCacheMock = new Mock<ISubdomainCache>(MockBehavior.Strict);
        var chaosAddressCacheMock = new Mock<IChaosAddressCache>(MockBehavior.Strict);

        // Register mocks
        services.AddSingleton(messageStoreProviderMock.Object);
        services.AddSingleton(commanderMock.Object);
        services.AddSingleton(subdomainCacheMock.Object);
        services.AddSingleton(chaosAddressCacheMock.Object);

        // Register background job channels
        services.AddSingleton(Channel.CreateUnbounded<CampaignCaptureJob>());
        services.AddSingleton(Channel.CreateUnbounded<ChaosAddressReceivedJob>());

        var provider = services.BuildServiceProvider();

        return (provider, new MockServices(
            messageStoreProviderMock,
            commanderMock,
            subdomainCacheMock,
            chaosAddressCacheMock));
    }

    private record MockServices(
        Mock<IMessageStoreProvider> MessageStoreProviderMock,
        Mock<ICommander> CommanderMock,
        Mock<ISubdomainCache> SubdomainCacheMock,
        Mock<IChaosAddressCache> ChaosAddressCacheMock);
}
