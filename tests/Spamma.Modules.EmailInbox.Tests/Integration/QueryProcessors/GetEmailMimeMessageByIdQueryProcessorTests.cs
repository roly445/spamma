using System.IO.Compression;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MimeKit;
using Spamma.Modules.EmailInbox.Client.Application.Queries;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;
using Spamma.Modules.EmailInbox.Infrastructure.Services;
using Spamma.Modules.EmailInbox.Tests.Builders;

namespace Spamma.Modules.EmailInbox.Tests.Integration.QueryProcessors;

public class GetEmailMimeMessageByIdQueryProcessorTests : QueryProcessorIntegrationTestBase
{
    public GetEmailMimeMessageByIdQueryProcessorTests()
        : base()
    {
    }

    [Fact]
    public async Task Handle_WithValidEmailId_ReturnsCompressedBase64MimeMessage()
    {
        var messageId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var messageStoreProvider = this.ServiceProvider.GetRequiredService<IMessageStoreProvider>();

        var email = EmailLookupTestFactory.Create(
            id: messageId,
            subdomainId: subdomainId,
            domainId: domainId,
            subject: "Test Email with MIME",
            sentAt: DateTime.UtcNow,
            isFavorite: false,
            emailAddresses: new List<EmailAddress>());
        this.Session.Store(email);
        await this.Session.SaveChangesAsync();

        var mimeMessage = new MimeMessage
        {
            Subject = "Test Email with MIME",
            From = { new MailboxAddress("Sender Name", "sender@example.com") },
            To = { new MailboxAddress("Recipient Name", "recipient@example.com") },
            Body = new TextPart("plain")
            {
                Text = "This is the email body content for MIME message testing.",
            },
        };

        var storeResult = await messageStoreProvider.StoreMessageContentAsync(messageId, mimeMessage);
        storeResult.IsSuccess.Should().BeTrue("MIME message should be stored successfully");

        var query = new GetEmailMimeMessageByIdQuery(messageId);

        var result = await this.Sender.Send(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Data.Should().NotBeNull();
        result.Data!.FileContent.Should().NotBeNullOrEmpty();

        var act = () => Convert.FromBase64String(result.Data.FileContent);
        act.Should().NotThrow("FileContent should be valid base64");

        var compressedBytes = Convert.FromBase64String(result.Data.FileContent);
        using var compressedStream = new MemoryStream(compressedBytes);
        using var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress);
        using var decompressedStream = new MemoryStream();
        await gzipStream.CopyToAsync(decompressedStream);
        decompressedStream.Seek(0, SeekOrigin.Begin);

        var decompressedMessage = await MimeMessage.LoadAsync(decompressedStream);
        decompressedMessage.Subject.Should().Be("Test Email with MIME");
        decompressedMessage.From.Mailboxes.First().Address.Should().Be("sender@example.com");
    }

    [Fact]
    public async Task Handle_WithNonExistentEmailId_ReturnsFailed()
    {
        var nonExistentEmailId = Guid.NewGuid();
        var query = new GetEmailMimeMessageByIdQuery(nonExistentEmailId);

        var result = await this.Sender.Send(query, CancellationToken.None);

        result.Should().NotBeNull();
        var act = () => result.Data;
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_WithMultipartMessage_ReturnsCompressedMimeMessage()
    {
        var messageId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var messageStoreProvider = this.ServiceProvider.GetRequiredService<IMessageStoreProvider>();

        var email = EmailLookupTestFactory.Create(
            id: messageId,
            subdomainId: subdomainId,
            domainId: domainId,
            subject: "Multipart Email",
            sentAt: DateTime.UtcNow,
            isFavorite: false,
            emailAddresses: new List<EmailAddress>());
        this.Session.Store(email);
        await this.Session.SaveChangesAsync();

        var multipart = new Multipart("mixed");
        multipart.Add(new TextPart("plain")
        {
            Text = "Plain text body",
        });
        multipart.Add(new TextPart("html")
        {
            Text = "<html><body><h1>HTML Body</h1></body></html>",
        });

        var mimeMessage = new MimeMessage
        {
            Subject = "Multipart Email",
            From = { new MailboxAddress("Multi Sender", "multi@example.com") },
            To = { new MailboxAddress("Multi Recipient", "recipient@example.com") },
            Body = multipart,
        };

        var storeResult = await messageStoreProvider.StoreMessageContentAsync(messageId, mimeMessage);
        storeResult.IsSuccess.Should().BeTrue();

        var query = new GetEmailMimeMessageByIdQuery(messageId);

        var result = await this.Sender.Send(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Data.Should().NotBeNull();
        result.Data!.FileContent.Should().NotBeNullOrEmpty();

        var compressedBytes = Convert.FromBase64String(result.Data.FileContent);
        using var compressedStream = new MemoryStream(compressedBytes);
        using var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress);
        using var decompressedStream = new MemoryStream();
        await gzipStream.CopyToAsync(decompressedStream);
        decompressedStream.Seek(0, SeekOrigin.Begin);

        var decompressedMessage = await MimeMessage.LoadAsync(decompressedStream);
        decompressedMessage.Subject.Should().Be("Multipart Email");
        decompressedMessage.Body.Should().BeOfType<Multipart>();
    }

    [Fact]
    public async Task Handle_WithLargeMessage_ReturnsCompressedData()
    {
        var messageId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var messageStoreProvider = this.ServiceProvider.GetRequiredService<IMessageStoreProvider>();

        var email = EmailLookupTestFactory.Create(
            id: messageId,
            subdomainId: subdomainId,
            domainId: domainId,
            subject: "Large Email Message",
            sentAt: DateTime.UtcNow,
            isFavorite: false,
            emailAddresses: new List<EmailAddress>());
        this.Session.Store(email);
        await this.Session.SaveChangesAsync();

        var largeText = new string('A', 100 * 1024);

        var mimeMessage = new MimeMessage
        {
            Subject = "Large Email Message",
            From = { new MailboxAddress("Large Sender", "large@example.com") },
            To = { new MailboxAddress("Large Recipient", "recipient@example.com") },
            Body = new TextPart("plain")
            {
                Text = largeText,
            },
        };

        var storeResult = await messageStoreProvider.StoreMessageContentAsync(messageId, mimeMessage);
        storeResult.IsSuccess.Should().BeTrue();

        var query = new GetEmailMimeMessageByIdQuery(messageId);

        var result = await this.Sender.Send(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Data.Should().NotBeNull();
        result.Data!.FileContent.Should().NotBeNullOrEmpty();

        var compressedBytes = Convert.FromBase64String(result.Data.FileContent);
        compressedBytes.Length.Should().BeLessThan(100 * 1024, "Compressed data should be smaller than original 100KB text");

        using var compressedStream = new MemoryStream(compressedBytes);
        using var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress);
        using var decompressedStream = new MemoryStream();
        await gzipStream.CopyToAsync(decompressedStream);
        decompressedStream.Seek(0, SeekOrigin.Begin);

        var decompressedMessage = await MimeMessage.LoadAsync(decompressedStream);
        decompressedMessage.Subject.Should().Be("Large Email Message");
        var textBody = (TextPart?)decompressedMessage.Body;

        textBody.Should().NotBeNull();
        textBody!.Text.Should().NotBeNullOrEmpty();
        textBody.Text!.Should().Contain("AAAAAAAAAAAAAAAA", "Text should contain large repeated pattern");
        textBody.Text.Length.Should().BeGreaterThanOrEqualTo(100 * 1024, "Text should be at least 100KB");
    }
}