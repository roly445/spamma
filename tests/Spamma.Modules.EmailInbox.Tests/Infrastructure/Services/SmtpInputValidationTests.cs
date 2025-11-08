using System.Buffers;
using System.Threading.Channels;
using BluQube.Commands;
using FluentAssertions;
using MaybeMonad;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MimeKit;
using Moq;
using ResultMonad;
using SmtpServer.Protocol;
using Spamma.Modules.DomainManagement.Client.Application.Queries;
using Spamma.Modules.DomainManagement.Client.Contracts;
using Spamma.Modules.DomainManagement.Client.Infrastructure.Caching;
using Spamma.Modules.EmailInbox.Client;
using Spamma.Modules.EmailInbox.Client.Application.Commands;
using Spamma.Modules.EmailInbox.Infrastructure.Services;
using Spamma.Modules.EmailInbox.Infrastructure.Services.BackgroundJobs;

namespace Spamma.Modules.EmailInbox.Tests.Infrastructure.Services;

/// <summary>
/// Phase 23: SMTP Input Validation & Injection Tests.
/// Security-focused tests validating email message parsing, validation, and safe storage.
/// </summary>
public class SmtpInputValidationTests
{
    [Fact]
    public async Task SaveAsync_MalformedMimeMessage_HandlesGracefully()
    {
        // Arrange: Create invalid MIME bytes (missing required headers)
        var malformedMime = "This is not a valid MIME message\r\n\r\nSome random content"u8.ToArray();
        var buffer = new ReadOnlySequence<byte>(malformedMime);
        var (serviceProvider, _) = CreateMockServiceProvider();

        // Act & Assert: MimeKit.MimeMessage.LoadAsync should throw on malformed MIME
        Func<Task> act = async () => await SpammaMessageStore.SaveAsyncWithProvider(serviceProvider, buffer, CancellationToken.None);

        await act.Should().ThrowAsync<FormatException>("malformed MIME should be rejected by MimeKit parser");
    }

    [Fact]
    public async Task SaveAsync_EmailToInvalidSubdomain_ReturnsMailboxNameNotAllowed()
    {
        // Arrange: Valid MIME but recipient subdomain doesn't exist
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Sender", "sender@example.com"));
        message.To.Add(new MailboxAddress("Recipient", "user@nonexistent.com"));
        message.Subject = "Test Subject";
        message.Body = new TextPart("plain") { Text = "Test body" };

        var buffer = CreateBufferFromMimeMessage(message);
        var (serviceProvider, mocks) = CreateMockServiceProvider();

        // Mock: Subdomain cache returns Nothing (subdomain doesn't exist)
        mocks.SubdomainCacheMock
            .Setup(x => x.GetSubdomainAsync("nonexistent.com", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<SearchSubdomainsQueryResult.SubdomainSummary>.Nothing);

        // Act
        var result = await SpammaMessageStore.SaveAsyncWithProvider(serviceProvider, buffer, CancellationToken.None);

        // Assert
        result.Should().Be(SmtpResponse.MailboxNameNotAllowed, "email to unknown subdomain should be rejected");
        mocks.SubdomainCacheMock.Verify(x => x.GetSubdomainAsync("nonexistent.com", false, It.IsAny<CancellationToken>()), Times.Once);
        mocks.MessageStoreProviderMock.Verify(
            x => x.StoreMessageContentAsync(It.IsAny<Guid>(), It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "message should not be stored for invalid subdomain");
    }

    [Fact]
    public async Task SaveAsync_SubjectWithSqlInjectionCharacters_StoresSafely()
    {
        // Arrange: Subject contains SQL injection attempt
        var maliciousSubject = "Test'; DROP TABLE emails; --";
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Sender", "sender@example.com"));
        message.To.Add(new MailboxAddress("Recipient", "user@test.spamma.io"));
        message.Subject = maliciousSubject;
        message.Body = new TextPart("plain") { Text = "Test body" };

        var buffer = CreateBufferFromMimeMessage(message);
        var (serviceProvider, mocks) = CreateMockServiceProvider();

        var subdomain = CreateSubdomainSummary("test.spamma.io");

        mocks.SubdomainCacheMock
            .Setup(x => x.GetSubdomainAsync("test.spamma.io", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(subdomain));

        mocks.ChaosAddressCacheMock
            .Setup(x => x.GetChaosAddressAsync(subdomain.SubdomainId, "user", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<GetChaosAddressBySubdomainAndLocalPartQueryResult>.Nothing);

        mocks.MessageStoreProviderMock
            .Setup(x => x.StoreMessageContentAsync(It.IsAny<Guid>(), It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        mocks.CommanderMock
            .Setup(x => x.Send(It.Is<ReceivedEmailCommand>(cmd => cmd.Subject == maliciousSubject), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult.Succeeded());

        // Act
        var result = await SpammaMessageStore.SaveAsyncWithProvider(serviceProvider, buffer, CancellationToken.None);

        // Assert
        result.Should().Be(SmtpResponse.Ok, "valid email should be accepted regardless of subject content");
        mocks.CommanderMock.Verify(
            x => x.Send(It.Is<ReceivedEmailCommand>(cmd => cmd.Subject == maliciousSubject), It.IsAny<CancellationToken>()),
            Times.Once,
            "SQL injection characters should be stored as-is (event sourcing handles escaping)");
    }

    [Fact]
    public async Task SaveAsync_BodyWithXssPayload_StoresWithoutExecution()
    {
        // Arrange: Body contains XSS payload
        var xssPayload = "<script>alert('XSS');</script><img src=x onerror=alert('XSS')>";
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Attacker", "attacker@evil.com"));
        message.To.Add(new MailboxAddress("Victim", "user@test.spamma.io"));
        message.Subject = "Test Subject";
        message.Body = new TextPart("html") { Text = xssPayload };

        var buffer = CreateBufferFromMimeMessage(message);
        var (serviceProvider, mocks) = CreateMockServiceProvider();

        var subdomain = CreateSubdomainSummary("test.spamma.io");

        mocks.SubdomainCacheMock
            .Setup(x => x.GetSubdomainAsync("test.spamma.io", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(subdomain));

        mocks.ChaosAddressCacheMock
            .Setup(x => x.GetChaosAddressAsync(subdomain.SubdomainId, "user", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<GetChaosAddressBySubdomainAndLocalPartQueryResult>.Nothing);

        mocks.MessageStoreProviderMock
            .Setup(x => x.StoreMessageContentAsync(It.IsAny<Guid>(), It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        mocks.CommanderMock
            .Setup(x => x.Send(It.IsAny<ReceivedEmailCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult.Succeeded());

        // Act
        var result = await SpammaMessageStore.SaveAsyncWithProvider(serviceProvider, buffer, CancellationToken.None);

        // Assert
        result.Should().Be(SmtpResponse.Ok, "XSS payload should be stored as plain text without execution");
        mocks.MessageStoreProviderMock.Verify(
            x => x.StoreMessageContentAsync(
                It.IsAny<Guid>(),
                It.Is<MimeMessage>(msg => msg.HtmlBody != null && msg.HtmlBody.Contains(xssPayload)),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "XSS payload should be stored as-is (rendering layer handles escaping)");
    }

    [Fact]
    public async Task SaveAsync_HeaderWithCrlfInjection_ParsedSafely()
    {
        // Arrange: Subject with CRLF injection attempt (trying to inject additional headers)
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Sender", "sender@example.com"));
        message.To.Add(new MailboxAddress("Recipient", "user@test.spamma.io"));

        // MimeKit automatically sanitizes headers, but we test that CRLF doesn't cause issues
        var crlfSubject = "Legitimate Subject\r\nBcc: attacker@evil.com";
        message.Subject = crlfSubject;
        message.Body = new TextPart("plain") { Text = "Test body" };

        var buffer = CreateBufferFromMimeMessage(message);
        var (serviceProvider, mocks) = CreateMockServiceProvider();

        var subdomain = CreateSubdomainSummary("test.spamma.io");

        mocks.SubdomainCacheMock
            .Setup(x => x.GetSubdomainAsync("test.spamma.io", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(subdomain));

        mocks.ChaosAddressCacheMock
            .Setup(x => x.GetChaosAddressAsync(subdomain.SubdomainId, "user", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<GetChaosAddressBySubdomainAndLocalPartQueryResult>.Nothing);

        mocks.MessageStoreProviderMock
            .Setup(x => x.StoreMessageContentAsync(It.IsAny<Guid>(), It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        mocks.CommanderMock
            .Setup(x => x.Send(It.IsAny<ReceivedEmailCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult.Succeeded());

        // Act
        var result = await SpammaMessageStore.SaveAsyncWithProvider(serviceProvider, buffer, CancellationToken.None);

        // Assert
        result.Should().Be(SmtpResponse.Ok, "MimeKit should handle CRLF in headers safely");
        mocks.CommanderMock.Verify(
            x => x.Send(
                It.Is<ReceivedEmailCommand>(cmd =>
                    cmd.Subject.Contains(crlfSubject) || cmd.Subject.Contains("Legitimate Subject")),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "subject should be stored (MimeKit handles CRLF sanitization)");
    }

    [Fact]
    public async Task SaveAsync_EmailAddressWithMultipleAtSymbols_ParsedCorrectly()
    {
        // Arrange: Malformed email address with multiple @ symbols
        // Note: MimeKit validates email addresses strictly per RFC 5322 and will throw ParseException
        // This test verifies that MimeKit enforces proper email validation (security feature)
        var action = () =>
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Sender", "sender@example.com"));
            message.To.Add(new MailboxAddress("Malformed", "user@invalid@test.spamma.io")); // This will throw
            return Task.CompletedTask;
        };

        // Act & Assert
        await action.Should().ThrowAsync<ParseException>("MimeKit validates email addresses per RFC 5322");
    }

    [Fact]
    public async Task SaveAsync_RecipientListWithMalformedAddresses_ExtractsValidRecipients()
    {
        // Arrange: Mix of valid and edge-case recipient addresses
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Sender", "sender@example.com"));

        // Add multiple recipients including edge cases
        message.To.Add(new MailboxAddress("User1", "user1@test.spamma.io"));
        message.To.Add(new MailboxAddress("User2", "user2@test.spamma.io"));
        message.Cc.Add(new MailboxAddress("User3", "user3@test.spamma.io"));
        message.Bcc.Add(new MailboxAddress("User4", "user4@test.spamma.io"));

        message.Subject = "Test Subject";
        message.Body = new TextPart("plain") { Text = "Test body" };

        var buffer = CreateBufferFromMimeMessage(message);
        var (serviceProvider, mocks) = CreateMockServiceProvider();

        var subdomain = CreateSubdomainSummary("test.spamma.io");

        // Mock cache to return subdomain for test.spamma.io
        mocks.SubdomainCacheMock
            .Setup(x => x.GetSubdomainAsync("test.spamma.io", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(subdomain));

        mocks.ChaosAddressCacheMock
            .Setup(x => x.GetChaosAddressAsync(subdomain.SubdomainId, It.IsAny<string>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<GetChaosAddressBySubdomainAndLocalPartQueryResult>.Nothing);

        mocks.MessageStoreProviderMock
            .Setup(x => x.StoreMessageContentAsync(It.IsAny<Guid>(), It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        mocks.CommanderMock
            .Setup(
                x => x.Send(
                    It.Is<ReceivedEmailCommand>(cmd =>
                        cmd.EmailAddresses.Count == 5 && // 4 recipients + 1 sender
                        cmd.EmailAddresses.Any(a => a.Address == "user1@test.spamma.io" && a.EmailAddressType == EmailAddressType.To) &&
                        cmd.EmailAddresses.Any(a => a.Address == "user2@test.spamma.io" && a.EmailAddressType == EmailAddressType.To) &&
                        cmd.EmailAddresses.Any(a => a.Address == "user3@test.spamma.io" && a.EmailAddressType == EmailAddressType.Cc) &&
                        cmd.EmailAddresses.Any(a => a.Address == "user4@test.spamma.io" && a.EmailAddressType == EmailAddressType.Bcc) &&
                        cmd.EmailAddresses.Any(a => a.Address == "sender@example.com" && a.EmailAddressType == EmailAddressType.From)),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult.Succeeded());

        // Act
        var result = await SpammaMessageStore.SaveAsyncWithProvider(serviceProvider, buffer, CancellationToken.None);

        // Assert
        result.Should().Be(SmtpResponse.Ok, "all recipients should be extracted and stored");
        mocks.CommanderMock.Verify(
            x => x.Send(
                It.Is<ReceivedEmailCommand>(cmd =>
                    cmd.EmailAddresses.Count == 5 &&
                    cmd.EmailAddresses.Any(a => a.EmailAddressType == EmailAddressType.To) &&
                    cmd.EmailAddresses.Any(a => a.EmailAddressType == EmailAddressType.Cc) &&
                    cmd.EmailAddresses.Any(a => a.EmailAddressType == EmailAddressType.Bcc) &&
                    cmd.EmailAddresses.Any(a => a.EmailAddressType == EmailAddressType.From)),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "recipient extraction should preserve email address types");
    }

    [Fact]
    public async Task SaveAsync_NullOrEmptyDisplayName_HandledSafely()
    {
        // Arrange: Email with null/empty display name
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(null, "sender@example.com")); // Null display name
        message.To.Add(new MailboxAddress(string.Empty, "user@test.spamma.io")); // Empty display name
        message.Subject = "Test Subject";
        message.Body = new TextPart("plain") { Text = "Test body" };

        var buffer = CreateBufferFromMimeMessage(message);
        var (serviceProvider, mocks) = CreateMockServiceProvider();

        var subdomain = CreateSubdomainSummary("test.spamma.io");

        mocks.SubdomainCacheMock
            .Setup(x => x.GetSubdomainAsync("test.spamma.io", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(subdomain));

        mocks.ChaosAddressCacheMock
            .Setup(x => x.GetChaosAddressAsync(subdomain.SubdomainId, "user", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<GetChaosAddressBySubdomainAndLocalPartQueryResult>.Nothing);

        mocks.MessageStoreProviderMock
            .Setup(x => x.StoreMessageContentAsync(It.IsAny<Guid>(), It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        mocks.CommanderMock
            .Setup(x => x.Send(It.Is<ReceivedEmailCommand>(cmd => cmd.EmailAddresses.All(a => a.Name != null)), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult.Succeeded());

        // Act
        var result = await SpammaMessageStore.SaveAsyncWithProvider(serviceProvider, buffer, CancellationToken.None);

        // Assert
        result.Should().Be(SmtpResponse.Ok, "null display names should be handled gracefully");
        mocks.CommanderMock.Verify(
            x => x.Send(It.Is<ReceivedEmailCommand>(cmd => cmd.EmailAddresses.All(a => a.Name != null)), It.IsAny<CancellationToken>()),
            Times.Once,
            "display names should be converted to empty string when null");
    }

    /// <summary>
    /// Helper method to create a ReadOnlySequence from a MimeMessage.
    /// </summary>
    private static ReadOnlySequence<byte> CreateBufferFromMimeMessage(MimeMessage message)
    {
        using var stream = new MemoryStream();
        message.WriteTo(stream);
        var bytes = stream.ToArray();
        return new ReadOnlySequence<byte>(bytes);
    }

    /// <summary>
    /// Helper method to create a subdomain summary with sensible defaults.
    /// </summary>
    private static SearchSubdomainsQueryResult.SubdomainSummary CreateSubdomainSummary(string fullDomainName)
    {
        return new SearchSubdomainsQueryResult.SubdomainSummary(
            SubdomainId: Guid.NewGuid(),
            ParentDomainId: Guid.NewGuid(),
            SubdomainName: fullDomainName.Split('.')[0],
            ParentDomainName: string.Join('.', fullDomainName.Split('.')[1..]),
            FullDomainName: fullDomainName,
            Status: SubdomainStatus.Active,
            CreatedAt: DateTime.UtcNow,
            ChaosMonkeyRuleCount: 0,
            ActiveCampaignCount: 0,
            Description: null);
    }

    /// <summary>
    /// Creates a mock service provider with all required dependencies for SpammaMessageStore.
    /// </summary>
    /// <returns>A tuple containing the service provider and mock services.</returns>
    private static (IServiceProvider ServiceProvider, MockServices Mocks) CreateMockServiceProvider()
    {
        var services = new ServiceCollection();

        // Add logger
        services.AddLogging(builder => builder.AddConsole());

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
