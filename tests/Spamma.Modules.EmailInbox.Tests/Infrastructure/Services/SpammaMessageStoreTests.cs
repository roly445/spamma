using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BluQube.Commands;
using BluQube.Queries;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MimeKit;
using Moq;
using ResultMonad;
using Spamma.Modules.Common;
using Spamma.Modules.DomainManagement.Client.Application.Commands.RecordChaosAddressReceived;
using Spamma.Modules.DomainManagement.Client.Application.Queries;
using Spamma.Modules.DomainManagement.Client.Contracts;
using Spamma.Modules.EmailInbox.Client.Application.Commands;
using Spamma.Modules.EmailInbox.Infrastructure.Services;
using Xunit;

namespace Spamma.Modules.EmailInbox.Tests.Infrastructure.Services;

public class SpammaMessageStoreTests
{
    [Fact]
    public async Task SaveAsync_FirstMatchingChaosAddress_ReturnsConfiguredSmtpCode()
    {
        // Arrange: first recipient unknown, second matches chaos address
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("sender", "sender@example.com"));
        message.To.Add(new MailboxAddress("user1", "user1@unknown.com"));
        message.To.Add(new MailboxAddress("chaos", "chaos@spamma.io"));

        var buffer = SerializeMimeMessage(message);

        var querier = new Mock<IQuerier>(MockBehavior.Strict);
        querier
            .SetupSequence(q => q.Send(It.IsAny<SearchSubdomainsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(QueryResult<SearchSubdomainsQueryResult>.Succeeded(new SearchSubdomainsQueryResult(Array.Empty<SearchSubdomainsQueryResult.SubdomainSummary>(), 0, 1, 10, 0)))
            .ReturnsAsync(QueryResult<SearchSubdomainsQueryResult>.Succeeded(new SearchSubdomainsQueryResult(new[] { new SearchSubdomainsQueryResult.SubdomainSummary(Guid.NewGuid(), Guid.NewGuid(), "spamma.io", "spamma.io", "spamma.io", SubdomainStatus.Active, DateTime.UtcNow, 0, 0, null) }, 1, 1, 10, 1)));

        var chaosAddressId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();
        querier
            .Setup(q => q.Send(It.IsAny<GetChaosAddressBySubdomainAndLocalPartQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(QueryResult<GetChaosAddressBySubdomainAndLocalPartQueryResult>.Succeeded(
                new GetChaosAddressBySubdomainAndLocalPartQueryResult(chaosAddressId, subdomainId, "chaos", SmtpResponseCode.MailboxUnavailable, true)));

        var commander = new Mock<ICommander>(MockBehavior.Strict);
        commander
            .Setup(c => c.Send(It.IsAny<RecordChaosAddressReceivedCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult.Succeeded());

        var messageProvider = new Mock<IMessageStoreProvider>(MockBehavior.Strict);

        var services = new ServiceCollection();
        services.AddSingleton(querier.Object);
        services.AddSingleton(commander.Object);
        services.AddSingleton(messageProvider.Object);
        services.AddSingleton<IInternalQueryStore, InternalQueryStoreStub>();
        services.AddLogging();

        var provider = services.BuildServiceProvider();

        // Act
        var result = await SpammaMessageStore.SaveAsyncWithProvider(provider, buffer, CancellationToken.None);

        // Assert
        Assert.Equal((int)SmtpResponseCode.MailboxUnavailable, GetNumericReplyCode(result));
    }

    [Fact]
    public async Task SaveAsync_CommanderThrows_BestEffortStillReturnsConfiguredCode()
    {
        // Arrange: chaos address present but commander throws
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("sender", "sender@example.com"));
        message.To.Add(new MailboxAddress("chaos", "chaos@spamma.io"));

        var buffer = SerializeMimeMessage(message);

        var querier = new Mock<IQuerier>(MockBehavior.Strict);
        querier
            .Setup(q => q.Send(It.IsAny<SearchSubdomainsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(QueryResult<SearchSubdomainsQueryResult>.Succeeded(new SearchSubdomainsQueryResult(new[] { new SearchSubdomainsQueryResult.SubdomainSummary(Guid.NewGuid(), Guid.NewGuid(), "spamma.io", "spamma.io", "spamma.io", SubdomainStatus.Active, DateTime.UtcNow, 0, 0, null) }, 1, 1, 10, 1)));

        var chaosAddressId = Guid.NewGuid();
        var chaosSubdomainId = Guid.NewGuid();
        querier
            .Setup(q => q.Send(It.IsAny<GetChaosAddressBySubdomainAndLocalPartQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(QueryResult<GetChaosAddressBySubdomainAndLocalPartQueryResult>.Succeeded(
                new GetChaosAddressBySubdomainAndLocalPartQueryResult(chaosAddressId, chaosSubdomainId, "chaos", SmtpResponseCode.MailboxUnavailable, true)));

        var commander = new Mock<ICommander>(MockBehavior.Strict);
        commander
            .Setup(c => c.Send(It.IsAny<RecordChaosAddressReceivedCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Simulated command failure"));

        var messageProvider = new Mock<IMessageStoreProvider>(MockBehavior.Strict);

        var services = new ServiceCollection();
        services.AddSingleton(querier.Object);
        services.AddSingleton(commander.Object);
        services.AddSingleton(messageProvider.Object);
        services.AddSingleton<IInternalQueryStore, InternalQueryStoreStub>();
        services.AddLogging();

        var provider = services.BuildServiceProvider();

        // Act
        var result = await SpammaMessageStore.SaveAsyncWithProvider(provider, buffer, CancellationToken.None);

        // Assert: numeric SMTP reply equals configured enum value despite commander throwing
        Assert.Equal((int)SmtpResponseCode.MailboxUnavailable, GetNumericReplyCode(result));
    }

    [Fact]
    public async Task SaveAsync_ChecksRecipientsInToCcBccOrder_FirstChaosMatchWins()
    {
        // Arrange: Cc has chaos address, To has different domain - Cc should be checked first and win
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("sender", "sender@example.com"));
        message.To.Add(new MailboxAddress("user", "user@other.com"));
        message.Cc.Add(new MailboxAddress("chaos", "chaos@spamma.io"));

        var buffer = SerializeMimeMessage(message);

        var querier = new Mock<IQuerier>(MockBehavior.Strict);
        querier
            .SetupSequence(q => q.Send(It.IsAny<SearchSubdomainsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(QueryResult<SearchSubdomainsQueryResult>.Succeeded(new SearchSubdomainsQueryResult(Array.Empty<SearchSubdomainsQueryResult.SubdomainSummary>(), 0, 1, 10, 0))) // To: user@other.com - no match
            .ReturnsAsync(QueryResult<SearchSubdomainsQueryResult>.Succeeded(new SearchSubdomainsQueryResult(new[] { new SearchSubdomainsQueryResult.SubdomainSummary(Guid.NewGuid(), Guid.NewGuid(), "spamma.io", "spamma.io", "spamma.io", SubdomainStatus.Active, DateTime.UtcNow, 0, 0, null) }, 1, 1, 10, 1))); // Cc: chaos@spamma.io - match

        var chaosId = Guid.NewGuid();
        var chaosSubId = Guid.NewGuid();
        querier
            .Setup(q => q.Send(It.IsAny<GetChaosAddressBySubdomainAndLocalPartQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(QueryResult<GetChaosAddressBySubdomainAndLocalPartQueryResult>.Succeeded(
                new GetChaosAddressBySubdomainAndLocalPartQueryResult(chaosId, chaosSubId, "chaos", SmtpResponseCode.MailboxUnavailable, true)));

        var commander = new Mock<ICommander>(MockBehavior.Strict);
        commander
            .Setup(c => c.Send(It.IsAny<RecordChaosAddressReceivedCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult.Succeeded());

        var messageProvider = new Mock<IMessageStoreProvider>(MockBehavior.Strict);

        var services = new ServiceCollection();
        services.AddSingleton(querier.Object);
        services.AddSingleton(commander.Object);
        services.AddSingleton(messageProvider.Object);
        services.AddSingleton<IInternalQueryStore, InternalQueryStoreStub>();
        services.AddLogging();

        var provider = services.BuildServiceProvider();

        // Act
        var result = await SpammaMessageStore.SaveAsyncWithProvider(provider, buffer, CancellationToken.None);

        // Assert: Cc recipient checked first, chaos address found and returns configured code
        Assert.Equal((int)SmtpResponseCode.MailboxUnavailable, GetNumericReplyCode(result));
    }

    [Fact]
    public async Task SaveAsync_NoChaosMatchButSubdomainFound_PersistsMessageAndDispatchesCommand()
    {
        // Arrange: subdomain found but no chaos address, should persist message and dispatch ReceivedEmailCommand
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("sender", "sender@example.com"));
        message.To.Add(new MailboxAddress("user", "user@spamma.io"));
        message.Subject = "Test Message";

        var buffer = SerializeMimeMessage(message);

        var querier = new Mock<IQuerier>(MockBehavior.Strict);
        querier
            .Setup(q => q.Send(It.IsAny<SearchSubdomainsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(QueryResult<SearchSubdomainsQueryResult>.Succeeded(new SearchSubdomainsQueryResult(new[] { new SearchSubdomainsQueryResult.SubdomainSummary(Guid.NewGuid(), Guid.NewGuid(), "spamma.io", "spamma.io", "spamma.io", SubdomainStatus.Active, DateTime.UtcNow, 0, 0, null) }, 1, 1, 10, 1)));

        querier
            .Setup(q => q.Send(It.IsAny<GetChaosAddressBySubdomainAndLocalPartQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(QueryResult<GetChaosAddressBySubdomainAndLocalPartQueryResult>.Failed()); // No chaos address found

        var commander = new Mock<ICommander>(MockBehavior.Strict);
        commander
            .Setup(c => c.Send(It.IsAny<RecordChaosAddressReceivedCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult.Succeeded()); // Should not be called

        commander
            .Setup(c => c.Send(It.IsAny<ReceivedEmailCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult.Succeeded());

        var messageProvider = new Mock<IMessageStoreProvider>(MockBehavior.Strict);
        messageProvider
            .Setup(mp => mp.StoreMessageContentAsync(It.IsAny<Guid>(), It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        var services = new ServiceCollection();
        services.AddSingleton(querier.Object);
        services.AddSingleton(commander.Object);
        services.AddSingleton(messageProvider.Object);
        services.AddSingleton<IInternalQueryStore, InternalQueryStoreStub>();
        services.AddLogging();

        var provider = services.BuildServiceProvider();

        // Act
        var result = await SpammaMessageStore.SaveAsyncWithProvider(provider, buffer, CancellationToken.None);

        // Assert: message persisted and command dispatched, returns Ok
        Assert.Equal(250, GetNumericReplyCode(result)); // SMTP OK response
        messageProvider.Verify(mp => mp.StoreMessageContentAsync(It.IsAny<Guid>(), It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        commander.Verify(c => c.Send(It.IsAny<ReceivedEmailCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        commander.Verify(c => c.Send(It.IsAny<RecordChaosAddressReceivedCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SaveAsync_NoSubdomainFound_ReturnsMailboxNameNotAllowed()
    {
        // Arrange: no subdomain found for any recipient
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("sender", "sender@example.com"));
        message.To.Add(new MailboxAddress("user", "user@unknown.com"));

        var buffer = SerializeMimeMessage(message);

        var querier = new Mock<IQuerier>(MockBehavior.Strict);
        querier
            .Setup(q => q.Send(It.IsAny<SearchSubdomainsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(QueryResult<SearchSubdomainsQueryResult>.Succeeded(new SearchSubdomainsQueryResult(Array.Empty<SearchSubdomainsQueryResult.SubdomainSummary>(), 0, 1, 10, 0)));

        var commander = new Mock<ICommander>(MockBehavior.Strict);
        var messageProvider = new Mock<IMessageStoreProvider>(MockBehavior.Strict);

        var services = new ServiceCollection();
        services.AddSingleton(querier.Object);
        services.AddSingleton(commander.Object);
        services.AddSingleton(messageProvider.Object);
        services.AddSingleton<IInternalQueryStore, InternalQueryStoreStub>();
        services.AddLogging();

        var provider = services.BuildServiceProvider();

        // Act
        var result = await SpammaMessageStore.SaveAsyncWithProvider(provider, buffer, CancellationToken.None);

        // Assert: returns MailboxNameNotAllowed (enum)
        Assert.Equal((int)SmtpResponseCode.MailboxNameNotAllowed, GetNumericReplyCode(result));

        messageProvider.Verify(mp => mp.StoreMessageContentAsync(It.IsAny<Guid>(), It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        commander.Verify(c => c.Send(It.IsAny<ReceivedEmailCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static ReadOnlySequence<byte> SerializeMimeMessage(MimeMessage message)
    {
        using var ms = new MemoryStream();
        message.WriteTo(ms);
        return new ReadOnlySequence<byte>(ms.ToArray());
    }

    private static int GetNumericReplyCode(object smtpResponse)
    {
        var t = smtpResponse.GetType();
        var prop = t.GetProperty("ReplyCode") ?? t.GetProperty("Reply") ?? t.GetProperty("Code");
        if (prop != null)
        {
            return Convert.ToInt32(prop.GetValue(smtpResponse));
        }

        var field = t.GetField("_replyCode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            return Convert.ToInt32(field.GetValue(smtpResponse));
        }

        throw new InvalidOperationException("Cannot read reply code from SmtpResponse");
    }

    private sealed class InternalQueryStoreStub : IInternalQueryStore
    {
        public Result AddReferenceForObject(object obj) => Result.Ok();

        public bool IsStoringReferenceForObject(object obj) => false;
    }
}
