using FluentAssertions;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using MimeKit;
using Moq;
using Spamma.Modules.Common.Application.Contracts;
using Spamma.Modules.EmailInbox.Client.Application.Queries;
using Spamma.Modules.EmailInbox.Client.Contracts;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;
using Spamma.Modules.EmailInbox.Infrastructure.Services;

namespace Spamma.Modules.EmailInbox.Tests.Integration;

public class MessageSearchMetadataBackfillTests : QueryProcessorIntegrationTestBase
{
    [Fact]
    public async Task BackfillBatch_IndexesExistingMimeAndReportsMissingFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), $"spamma-body-search-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var subdomainId = Guid.NewGuid();
            var indexedId = Guid.NewGuid();
            var inlineId = Guid.NewGuid();
            var missingId = Guid.NewGuid();
            var indexedBodyMissingMimeId = Guid.NewGuid();
            this.HttpContextAccessor.AddSubdomainClaim(subdomainId);
            foreach (var id in new[] { indexedId, inlineId, missingId, indexedBodyMissingMimeId })
            {
                this.Session.Store(new EmailLookup
                {
                    Id = id,
                    DomainId = Guid.NewGuid(),
                    SubdomainId = subdomainId,
                    Subject = "Old message",
                    SearchableBody = id == indexedBodyMissingMimeId ? "preservedwords" : null,
                    SentAt = DateTimeOffset.UtcNow,
                    EmailAddresses = [new("sender@example.com", "Sender", EmailAddressType.From)],
                });
            }

            await this.Session.SaveChangesAsync();

            var environment = new Mock<IHostEnvironment>();
            environment.SetupGet(host => host.ContentRootPath).Returns(root);
            var messageStore = new LocalMessageStoreProvider(
                environment.Object, NullLogger<LocalMessageStoreProvider>.Instance,
                new DirectoryWrapper(), new FileWrapper());
            var body = new BodyBuilder { TextBody = "A backfillquasar document" };
            body.Attachments.Add("report.txt", System.Text.Encoding.UTF8.GetBytes("report"));
            var message = new MimeMessage
            {
                Body = body.ToMessageBody(),
            };
            await messageStore.StoreMessageContentAsync(indexedId, message);
            var inlineBody = new BodyBuilder { HtmlBody = "<p>Inline only</p>" };
            inlineBody.LinkedResources.Add("logo.png", new byte[] { 1, 2, 3 });
            await messageStore.StoreMessageContentAsync(inlineId, new MimeMessage { Body = inlineBody.ToMessageBody() });

            var processed = await MessageSearchMetadataBackfillService.BackfillBatchAsync(
                this.Session, messageStore, environment.Object, NullLogger.Instance, CancellationToken.None);

            processed.Should().Be(4);
            var result = await this.Sender.Send(
                new SearchEmailsQuery(SearchText: "body:backfillquasar"), CancellationToken.None);
            result.Data.Items.Should().ContainSingle();
            result.Data.Items[0].EmailId.Should().Be(indexedId);

            var attachments = await this.Sender.Send(
                new SearchEmailsQuery(SearchText: "has:attachment"), CancellationToken.None);
            attachments.Data.Items.Should().ContainSingle().Which.EmailId.Should().Be(indexedId);

            await using var querySession = this.ServiceProvider.GetRequiredService<IDocumentStore>().QuerySession();
            var missing = await querySession.LoadAsync<EmailLookup>(missingId);
            missing!.SearchableBody.Should().BeEmpty();
            missing.BodyIndexError.Should().Be("Stored EML is missing");
            missing.HasAttachment.Should().BeFalse();
            missing.AttachmentIndexError.Should().Be("Stored EML is missing");
            (await querySession.LoadAsync<EmailLookup>(inlineId))!.HasAttachment.Should().BeFalse();
            var indexedBodyMissingMime = await querySession.LoadAsync<EmailLookup>(indexedBodyMissingMimeId);
            indexedBodyMissingMime!.SearchableBody.Should().Be("preservedwords");
            indexedBodyMissingMime.HasAttachment.Should().BeFalse();
            indexedBodyMissingMime.AttachmentIndexError.Should().Be("Stored EML is missing");

            (await MessageSearchMetadataBackfillService.BackfillBatchAsync(
                this.Session, messageStore, environment.Object, NullLogger.Instance, CancellationToken.None))
                .Should().Be(0);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
