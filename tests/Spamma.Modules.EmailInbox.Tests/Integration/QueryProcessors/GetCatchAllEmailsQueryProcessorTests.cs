using FluentAssertions;
using Spamma.Modules.EmailInbox.Client.Application.Queries;
using Spamma.Modules.EmailInbox.Client.Contracts;
using Spamma.Modules.EmailInbox.Infrastructure.Constants;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

namespace Spamma.Modules.EmailInbox.Tests.Integration.QueryProcessors;

public class GetCatchAllEmailsQueryProcessorTests : QueryProcessorIntegrationTestBase
{
    [Fact]
    public async Task Handle_InvalidSearch_ReturnsFailure()
    {
        var result = await this.Sender.Send(new GetCatchAllEmailsQuery(SearchText: "has:image"), CancellationToken.None);

        result.Status.Should().Be(BluQube.Constants.QueryResultStatus.Failed);
    }

    [Fact]
    public async Task Handle_WithAttachmentFilter_ReturnsOnlyMatchingCatchAllMessages()
    {
        foreach (var (subject, hasAttachment) in new[]
                 {
                     ("Catch-all attached", (bool?)true),
                     ("Catch-all inline", (bool?)false),
                 })
        {
            this.Session.Store(new EmailLookup
            {
                Id = Guid.NewGuid(),
                DomainId = CatchAllConstants.DomainId,
                SubdomainId = CatchAllConstants.SubdomainId,
                Subject = subject,
                HasAttachment = hasAttachment,
                SentAt = DateTimeOffset.UtcNow,
                EmailAddresses = [new("sender@example.com", "Sender", EmailAddressType.From)],
            });
        }

        await this.Session.SaveChangesAsync();

        var result = await this.Sender.Send(new GetCatchAllEmailsQuery(SearchText: "has:attachment"), CancellationToken.None);

        result.Data.TotalCount.Should().Be(1);
        result.Data.Groups.Should().ContainSingle();
        result.Data.Groups[0].Emails.Should().ContainSingle().Which.Subject.Should().Be("Catch-all attached");
    }

    [Fact]
    public async Task Handle_WithBodyFilter_ReturnsMatchingCatchAllEmail()
    {
        this.Session.Store(new EmailLookup
        {
            Id = Guid.NewGuid(),
            DomainId = CatchAllConstants.DomainId,
            SubdomainId = CatchAllConstants.SubdomainId,
            Subject = "Unrelated subject",
            SearchableBody = "A nebularinvoice is ready",
            SentAt = DateTimeOffset.UtcNow,
            EmailAddresses = [new("sender@example.com", "Sender", EmailAddressType.From)],
        });
        await this.Session.SaveChangesAsync();

        var result = await this.Sender.Send(
            new GetCatchAllEmailsQuery(SearchText: "body:nebularinvoice"), CancellationToken.None);

        result.Data.TotalCount.Should().Be(1);
        result.Data.Groups.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_WithSearchText_ReturnsMatchingCatchAllEmails()
    {
        // Arrange
        var matchingEmail = new EmailLookup
        {
            Id = Guid.NewGuid(),
            DomainId = CatchAllConstants.DomainId,
            SubdomainId = CatchAllConstants.SubdomainId,
            Subject = "GitHub campaign alert",
            SentAt = DateTimeOffset.UtcNow,
            CatchAllSenderAddressId = Guid.NewGuid(),
            EmailAddresses =
            [
                new("noreply@github.com", "GitHub", EmailAddressType.From),
                new("anything@unknown.test", "Recipient", EmailAddressType.To),
            ],
        };

        var nonMatchingEmail = new EmailLookup
        {
            Id = Guid.NewGuid(),
            DomainId = CatchAllConstants.DomainId,
            SubdomainId = CatchAllConstants.SubdomainId,
            Subject = "Billing update",
            SentAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            CatchAllSenderAddressId = Guid.NewGuid(),
            EmailAddresses =
            [
                new("billing@example.com", "Billing", EmailAddressType.From),
                new("anything@unknown.test", "Recipient", EmailAddressType.To),
            ],
        };

        this.Session.Store(matchingEmail);
        this.Session.Store(nonMatchingEmail);
        await this.Session.SaveChangesAsync();

        var query = new GetCatchAllEmailsQuery(SearchText: "github");

        // Act
        var result = await this.Sender.Send(query, CancellationToken.None);

        // Assert
        result.Data.TotalCount.Should().Be(1);
        result.Data.Groups.Should().ContainSingle();
        result.Data.Groups[0].Emails.Should().ContainSingle();
        result.Data.Groups[0].Emails[0].Subject.Should().Be("GitHub campaign alert");
    }

    [Fact]
    public async Task Handle_WithCampaignEmail_ReturnsCampaignIdAndValue()
    {
        // Arrange
        var campaignId = Guid.NewGuid();
        var campaignValue = "catch-all-campaign";
        this.Session.Store(new CampaignSummary
        {
            CampaignId = campaignId,
            DomainId = CatchAllConstants.DomainId,
            SubdomainId = CatchAllConstants.SubdomainId,
            CampaignValue = campaignValue,
            FirstReceivedAt = DateTimeOffset.UtcNow,
            LastReceivedAt = DateTimeOffset.UtcNow,
            TotalCaptured = 1,
        });

        this.Session.Store(new EmailLookup
        {
            Id = Guid.NewGuid(),
            DomainId = CatchAllConstants.DomainId,
            SubdomainId = CatchAllConstants.SubdomainId,
            Subject = "Campaign Catch-all GitHub sender test 1",
            SentAt = DateTimeOffset.UtcNow,
            CampaignId = campaignId,
            CatchAllSenderAddressId = Guid.NewGuid(),
            EmailAddresses =
            [
                new("noreply@github.com", "GitHub", EmailAddressType.From),
                new("anything@unregistered-catchall.test", "Recipient", EmailAddressType.To),
            ],
        });
        await this.Session.SaveChangesAsync();

        // Act
        var result = await this.Sender.Send(new GetCatchAllEmailsQuery(), CancellationToken.None);

        // Assert
        result.Data.Groups.Should().ContainSingle();
        var email = result.Data.Groups[0].Emails.Should().ContainSingle().Subject;
        email.CampaignId.Should().Be(campaignId);
        email.CampaignValue.Should().Be(campaignValue);
    }
}
