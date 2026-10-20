using FluentAssertions;
using Spamma.Modules.EmailInbox.Client.Application.Queries;
using Spamma.Modules.EmailInbox.Client.Contracts;
using Spamma.Modules.EmailInbox.Domain.EmailAggregate.Events;
using Spamma.Modules.EmailInbox.Infrastructure.Constants;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;
using Spamma.Modules.EmailInbox.Tests.Builders;

namespace Spamma.Modules.EmailInbox.Tests.Integration.QueryProcessors;

public class SearchEmailsQueryProcessorTests : QueryProcessorIntegrationTestBase
{
    [Fact]
    public async Task Handle_WithAttachmentFilter_CombinesSearchPaginationAndSubdomainScope()
    {
        var visibleSubdomain = Guid.NewGuid();
        var hiddenSubdomain = Guid.NewGuid();
        this.HttpContextAccessor.AddSubdomainClaim(visibleSubdomain);
        var now = DateTime.UtcNow;
        foreach (var (subdomain, subject, hasAttachment, offset) in new[]
                 {
                     (visibleSubdomain, "Invoice one", (bool?)true, 0),
                     (visibleSubdomain, "Invoice two", (bool?)true, -1),
                     (visibleSubdomain, "Invoice no file", (bool?)false, -2),
                     (hiddenSubdomain, "Invoice hidden", (bool?)true, -3),
                 })
        {
            this.Session.Store(EmailLookupTestFactory.Create(
                Guid.NewGuid(), subdomain, Guid.NewGuid(), subject, now.AddMinutes(offset), false,
                [new("sender@example.com", "Sender", EmailAddressType.From)],
                hasAttachment: hasAttachment));
        }

        await this.Session.SaveChangesAsync();

        var result = await this.Sender.Send(new SearchEmailsQuery(SearchText: "has:attachment subject:invoice", Page: 2, PageSize: 1), CancellationToken.None);

        result.Data.TotalCount.Should().Be(2);
        result.Data.TotalPages.Should().Be(2);
        result.Data.Items.Should().ContainSingle().Which.Subject.Should().Be("Invoice two");
    }

    [Fact]
    public async Task Handle_ProjectedBody_IsRemovedFromSearchAfterDeletion()
    {
        var emailId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();
        this.HttpContextAccessor.AddSubdomainClaim(subdomainId);
        this.Session.Events.StartStream<Spamma.Modules.EmailInbox.Domain.EmailAggregate.Email>(
            emailId,
            new EmailReceived(
                emailId, Guid.NewGuid(), subdomainId, "Other subject", DateTimeOffset.UtcNow,
                [new("sender@example.com", "Sender", EmailAddressType.From)],
                SearchableBody: "Projectedbackfill nebula"));
        await this.Session.SaveChangesAsync();

        var query = new SearchEmailsQuery(SearchText: "body:projectedbackfill");
        (await this.Sender.Send(query, CancellationToken.None)).Data.Items.Should().ContainSingle();

        this.Session.Events.Append(emailId, new EmailDeleted(DateTime.UtcNow));
        await this.Session.SaveChangesAsync();

        (await this.Sender.Send(query, CancellationToken.None)).Data.Items.Should().BeEmpty();
    }

    [Theory]
    [InlineData("body:quasarbilling")]
    [InlineData("quasarbilling")]
    public async Task Handle_WithBodySearch_KeepsSubdomainScope(string searchText)
    {
        var visibleSubdomain = Guid.NewGuid();
        var hiddenSubdomain = Guid.NewGuid();
        this.HttpContextAccessor.AddSubdomainClaim(visibleSubdomain);

        foreach (var subdomainId in new[] { visibleSubdomain, hiddenSubdomain })
        {
            this.Session.Store(EmailLookupTestFactory.Create(
                Guid.NewGuid(), subdomainId, Guid.NewGuid(), "Unrelated subject",
                DateTime.UtcNow, false,
                [new("sender@example.com", "Sender", EmailAddressType.From)],
                searchableBody: "The quasarbilling report is ready"));
        }

        await this.Session.SaveChangesAsync();

        var result = await this.Sender.Send(new SearchEmailsQuery(SearchText: searchText), CancellationToken.None);

        result.Data.Items.Should().ContainSingle();
        result.Data.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WithQuotedBodyPhrase_CombinesSubjectAndBody()
    {
        var subdomainId = Guid.NewGuid();
        this.HttpContextAccessor.AddSubdomainClaim(subdomainId);
        this.Session.Store(EmailLookupTestFactory.Create(
            Guid.NewGuid(), subdomainId, Guid.NewGuid(), "Invoice",
            DateTime.UtcNow, false,
            [new("sender@example.com", "Sender", EmailAddressType.From)],
            searchableBody: "A cobalt invoice arrived"));
        this.Session.Store(EmailLookupTestFactory.Create(
            Guid.NewGuid(), subdomainId, Guid.NewGuid(), "Invoice",
            DateTime.UtcNow.AddMinutes(-1), false,
            [new("sender@example.com", "Sender", EmailAddressType.From)],
            searchableBody: "An invoice for cobalt arrived"));
        await this.Session.SaveChangesAsync();

        var result = await this.Sender.Send(
            new SearchEmailsQuery(SearchText: "subject:invoice body:\"cobalt invoice\""), CancellationToken.None);

        result.Data.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_WithBodyFilter_DoesNotMatchSubjectAlone()
    {
        var subdomainId = Guid.NewGuid();
        this.HttpContextAccessor.AddSubdomainClaim(subdomainId);
        this.Session.Store(EmailLookupTestFactory.Create(
            Guid.NewGuid(), subdomainId, Guid.NewGuid(), "Quasarbilling notice",
            DateTime.UtcNow, false,
            [new("sender@example.com", "Sender", EmailAddressType.From)],
            searchableBody: "Only unrelated body words"));
        await this.Session.SaveChangesAsync();

        var result = await this.Sender.Send(
            new SearchEmailsQuery(SearchText: "body:quasarbilling"), CancellationToken.None);

        result.Data.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithCombinedFilters_KeepsSubdomainScope()
    {
        var visibleSubdomain = Guid.NewGuid();
        var hiddenSubdomain = Guid.NewGuid();
        this.HttpContextAccessor.AddSubdomainClaim(visibleSubdomain);

        foreach (var subdomainId in new[] { visibleSubdomain, hiddenSubdomain })
        {
            this.Session.Store(EmailLookupTestFactory.Create(
                Guid.NewGuid(), subdomainId, Guid.NewGuid(), "Billing invoice",
                new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc), false,
                [
                    new("alice@example.com", "Alice", EmailAddressType.From),
                    new("bob@example.com", "Bob", EmailAddressType.To),
                ]));
        }

        await this.Session.SaveChangesAsync();

        var query = new SearchEmailsQuery(
            SearchText: "from:alice to:bob subject:\"billing invoice\" after:2026-01-01 before:2026-02-01");
        var result = await this.Sender.Send(query, CancellationToken.None);

        result.Data.Items.Should().ContainSingle();
        result.Data.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WithNoSearchText_ReturnsAllEmails()
    {
        // Arrange
        var subdomainId = Guid.NewGuid();
        var domainId = Guid.NewGuid();

        // Add subdomain claim so SearchEmailsQuery can access these emails
        this.HttpContextAccessor.AddSubdomainClaim(subdomainId);

        // Create 3 test emails
        for (int i = 0; i < 3; i++)
        {
            var email = EmailLookupTestFactory.Create(
                id: Guid.NewGuid(),
                subdomainId: subdomainId,
                domainId: domainId,
                subject: $"Test Email {i}",
                sentAt: DateTime.UtcNow.AddHours(-i),
                isFavorite: false,
                emailAddresses: new List<EmailAddress>
                {
                    new($"sender{i}@example.com", "Sender", EmailAddressType.From),
                    new($"recipient{i}@example.com", "Recipient", EmailAddressType.To),
                });

            this.Session.Store(email);
            this.PersistEmailAddresses(email);
        }

        await this.Session.SaveChangesAsync();

        var query = new SearchEmailsQuery();

        // Act
        var result = await this.Sender.Send(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().HaveCount(3);
        result.Data.TotalCount.Should().Be(3);
        result.Data.Page.Should().Be(1);
        result.Data.Items.Should().BeInDescendingOrder(e => e.ReceivedAt); // Should be sorted by WhenSent descending
    }

    [Fact]
    public async Task Handle_WithSearchText_ReturnsMatchingEmailsBySubject()
    {
        // Arrange
        var subdomainId = Guid.NewGuid();
        var domainId = Guid.NewGuid();

        // Add subdomain claim so SearchEmailsQuery can access these emails
        this.HttpContextAccessor.AddSubdomainClaim(subdomainId);

        // Create email matching search term in subject
        var matchingEmail = EmailLookupTestFactory.Create(
            id: Guid.NewGuid(),
            subdomainId: subdomainId,
            domainId: domainId,
            subject: "Important meeting notes",
            sentAt: DateTime.UtcNow,
            isFavorite: false,
            emailAddresses: new List<EmailAddress>
            {
                new("sender@example.com", "Sender", EmailAddressType.From),
                new("recipient@example.com", "Recipient", EmailAddressType.To),
            });

        // Create email NOT matching search term
        var nonMatchingEmail = EmailLookupTestFactory.Create(
            id: Guid.NewGuid(),
            subdomainId: subdomainId,
            domainId: domainId,
            subject: "Random topic",
            sentAt: DateTime.UtcNow.AddHours(-1),
            isFavorite: false,
            emailAddresses: new List<EmailAddress>
            {
                new("other@example.com", "Other", EmailAddressType.From),
                new("target@example.com", "Target", EmailAddressType.To),
            });

        this.Session.Store(matchingEmail);
        this.PersistEmailAddresses(matchingEmail);
        this.Session.Store(nonMatchingEmail);
        this.PersistEmailAddresses(nonMatchingEmail);
        await this.Session.SaveChangesAsync();

        var query = new SearchEmailsQuery(SearchText: "meeting");

        // Act
        var result = await this.Sender.Send(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().HaveCount(1);
        result.Data.Items[0].Subject.Should().Be("Important meeting notes");
        result.Data.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WithSearchText_ReturnsMatchingEmailsByEmailAddress()
    {
        // Arrange
        var subdomainId = Guid.NewGuid();
        var domainId = Guid.NewGuid();

        // Add subdomain claim so SearchEmailsQuery can access these emails
        this.HttpContextAccessor.AddSubdomainClaim(subdomainId);

        // Create email with matching email address
        var matchingEmail = EmailLookupTestFactory.Create(
            id: Guid.NewGuid(),
            subdomainId: subdomainId,
            domainId: domainId,
            subject: "Test Subject",
            sentAt: DateTime.UtcNow,
            isFavorite: false,
            emailAddresses: new List<EmailAddress>
            {
                new("john.doe@example.com", "John Doe", EmailAddressType.From),
                new("recipient@example.com", "Recipient", EmailAddressType.To),
            });

        // Create email with different addresses
        var nonMatchingEmail = EmailLookupTestFactory.Create(
            id: Guid.NewGuid(),
            subdomainId: subdomainId,
            domainId: domainId,
            subject: "Other Subject",
            sentAt: DateTime.UtcNow.AddHours(-1),
            isFavorite: false,
            emailAddresses: new List<EmailAddress>
            {
                new("other@example.com", "Other", EmailAddressType.From),
                new("target@example.com", "Target", EmailAddressType.To),
            });

        this.Session.Store(matchingEmail);
        this.PersistEmailAddresses(matchingEmail);
        this.Session.Store(nonMatchingEmail);
        this.PersistEmailAddresses(nonMatchingEmail);
        await this.Session.SaveChangesAsync();

        var query = new SearchEmailsQuery(SearchText: "john.doe");

        // Act
        var result = await this.Sender.Send(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().HaveCount(1);
        result.Data.Items[0].Subject.Should().Be("Test Subject");
    }

    [Fact]
    public async Task Handle_WithSearchText_ReturnsMatchingEmailsByName()
    {
        // Arrange
        var subdomainId = Guid.NewGuid();
        var domainId = Guid.NewGuid();

        // Add subdomain claim so SearchEmailsQuery can access these emails
        this.HttpContextAccessor.AddSubdomainClaim(subdomainId);

        // Create email with matching sender name
        var matchingEmail = EmailLookupTestFactory.Create(
            id: Guid.NewGuid(),
            subdomainId: subdomainId,
            domainId: domainId,
            subject: "Test Subject",
            sentAt: DateTime.UtcNow,
            isFavorite: false,
            emailAddresses: new List<EmailAddress>
            {
                new("sender@example.com", "Alice Smith", EmailAddressType.From),
                new("recipient@example.com", "Recipient", EmailAddressType.To),
            });

        // Create email with different name
        var nonMatchingEmail = EmailLookupTestFactory.Create(
            id: Guid.NewGuid(),
            subdomainId: subdomainId,
            domainId: domainId,
            subject: "Other Subject",
            sentAt: DateTime.UtcNow.AddHours(-1),
            isFavorite: false,
            emailAddresses: new List<EmailAddress>
            {
                new("other@example.com", "Bob Jones", EmailAddressType.From),
                new("target@example.com", "Target", EmailAddressType.To),
            });

        this.Session.Store(matchingEmail);
        this.PersistEmailAddresses(matchingEmail);
        this.Session.Store(nonMatchingEmail);
        this.PersistEmailAddresses(nonMatchingEmail);
        await this.Session.SaveChangesAsync();

        var query = new SearchEmailsQuery(SearchText: "alice");

        // Act
        var result = await this.Sender.Send(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().HaveCount(1);
        result.Data.Items[0].Subject.Should().Be("Test Subject");
    }

    [Fact]
    public async Task Handle_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var subdomainId = Guid.NewGuid();
        var domainId = Guid.NewGuid();

        // Add subdomain claim so SearchEmailsQuery can access these emails
        this.HttpContextAccessor.AddSubdomainClaim(subdomainId);

        // Create 25 emails
        for (int i = 0; i < 25; i++)
        {
            var email = EmailLookupTestFactory.Create(
                id: Guid.NewGuid(),
                subdomainId: subdomainId,
                domainId: domainId,
                subject: $"Email {i:D3}",
                sentAt: DateTime.UtcNow.AddMinutes(-i),
                isFavorite: false,
                emailAddresses: new List<EmailAddress>
                {
                    new($"sender{i}@example.com", "Sender", EmailAddressType.From),
                    new($"recipient{i}@example.com", "Recipient", EmailAddressType.To),
                });

            this.Session.Store(email);
            this.PersistEmailAddresses(email);
        }

        await this.Session.SaveChangesAsync();

        var query = new SearchEmailsQuery(Page: 2, PageSize: 10);

        // Act
        var result = await this.Sender.Send(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().HaveCount(10); // Second page should have 10 items
        result.Data.Page.Should().Be(2);
        result.Data.PageSize.Should().Be(10);
        result.Data.TotalCount.Should().Be(25);
        result.Data.TotalPages.Should().Be(3);
        result.Data.HasPreviousPage.Should().BeTrue();
        result.Data.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ExcludesDeletedEmails()
    {
        // Arrange
        var subdomainId = Guid.NewGuid();
        var domainId = Guid.NewGuid();

        // Add subdomain claim so SearchEmailsQuery can access these emails
        this.HttpContextAccessor.AddSubdomainClaim(subdomainId);

        // Create active email
        var activeEmail = EmailLookupTestFactory.Create(
            id: Guid.NewGuid(),
            subdomainId: subdomainId,
            domainId: domainId,
            subject: "Active Email",
            sentAt: DateTime.UtcNow,
            isFavorite: false,
            emailAddresses: new List<EmailAddress>
            {
                new("sender@example.com", "Sender", EmailAddressType.From),
                new("recipient@example.com", "Recipient", EmailAddressType.To),
            },
            deletedAt: null);

        // Create deleted email
        var deletedEmail = EmailLookupTestFactory.Create(
            id: Guid.NewGuid(),
            subdomainId: subdomainId,
            domainId: domainId,
            subject: "Deleted Email",
            sentAt: DateTime.UtcNow.AddHours(-1),
            isFavorite: false,
            emailAddresses: new List<EmailAddress>
            {
                new("sender@example.com", "Sender", EmailAddressType.From),
                new("recipient@example.com", "Recipient", EmailAddressType.To),
            },
            deletedAt: DateTime.UtcNow.AddMinutes(-30));

        this.Session.Store(activeEmail);
        this.PersistEmailAddresses(activeEmail);
        this.Session.Store(deletedEmail);
        this.PersistEmailAddresses(deletedEmail);
        await this.Session.SaveChangesAsync();

        var query = new SearchEmailsQuery();

        // Act
        var result = await this.Sender.Send(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().HaveCount(1);
        result.Data.Items[0].Subject.Should().Be("Active Email");
    }

    [Fact]
    public async Task Handle_ExcludesCatchAllEmails()
    {
        // Arrange
        var regularSubdomainId = Guid.NewGuid();
        var regularDomainId = Guid.NewGuid();

        this.HttpContextAccessor.AddSubdomainClaim(regularSubdomainId);
        this.HttpContextAccessor.AddSubdomainClaim(CatchAllConstants.SubdomainId);

        var regularEmail = EmailLookupTestFactory.Create(
            id: Guid.NewGuid(),
            subdomainId: regularSubdomainId,
            domainId: regularDomainId,
            subject: "Regular Inbox Email",
            sentAt: DateTime.UtcNow,
            isFavorite: false,
            emailAddresses: new List<EmailAddress>
            {
                new("sender@example.com", "Sender", EmailAddressType.From),
                new("recipient@example.com", "Recipient", EmailAddressType.To),
            });

        var catchAllEmail = new EmailLookup
        {
            Id = Guid.NewGuid(),
            DomainId = CatchAllConstants.DomainId,
            SubdomainId = CatchAllConstants.SubdomainId,
            Subject = "Catch-All Inbox Email",
            SentAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            CatchAllSenderAddressId = Guid.NewGuid(),
            EmailAddresses =
            [
                new("sender@example.com", "Sender", EmailAddressType.From),
                new("anything@unknown.example", "Recipient", EmailAddressType.To),
            ],
        };

        this.Session.Store(regularEmail);
        this.PersistEmailAddresses(regularEmail);
        this.Session.Store(catchAllEmail);
        this.PersistEmailAddresses(catchAllEmail);
        await this.Session.SaveChangesAsync();

        var query = new SearchEmailsQuery();

        // Act
        var result = await this.Sender.Send(query, CancellationToken.None);

        // Assert
        result.Data.Items.Should().ContainSingle();
        result.Data.Items[0].Subject.Should().Be("Regular Inbox Email");
    }

    [Fact]
    public async Task Handle_WithEmptyDatabase_ReturnsEmptyList()
    {
        // Arrange
        var query = new SearchEmailsQuery();

        // Act
        var result = await this.Sender.Send(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().BeEmpty();
        result.Data.TotalCount.Should().Be(0);
        result.Data.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task Handle_OrdersByWhenSent_Descending()
    {
        // Arrange
        var subdomainId = Guid.NewGuid();
        var domainId = Guid.NewGuid();

        // Add subdomain claim so SearchEmailsQuery can access these emails
        this.HttpContextAccessor.AddSubdomainClaim(subdomainId);

        var baseTime = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        // Create emails with specific timestamps
        var emails = new[]
        {
            new { Subject = "Oldest", WhenSent = baseTime.AddHours(1) },
            new { Subject = "Newest", WhenSent = baseTime.AddHours(3) },
            new { Subject = "Middle", WhenSent = baseTime.AddHours(2) },
        };

        foreach (var emailData in emails)
        {
            var email = EmailLookupTestFactory.Create(
            id: Guid.NewGuid(),
            subdomainId: subdomainId,
            domainId: domainId,
            subject: emailData.Subject,
            sentAt: emailData.WhenSent,
            isFavorite: false,
            emailAddresses: new List<EmailAddress>
                {
                    new("sender@example.com", "Sender", EmailAddressType.From),
                    new("recipient@example.com", "Recipient", EmailAddressType.To),
                });

            this.Session.Store(email);
            this.PersistEmailAddresses(email);
        }

        await this.Session.SaveChangesAsync();

        var query = new SearchEmailsQuery();

        // Act
        var result = await this.Sender.Send(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().HaveCount(3);
        result.Data.Items[0].Subject.Should().Be("Newest");
        result.Data.Items[1].Subject.Should().Be("Middle");
        result.Data.Items[2].Subject.Should().Be("Oldest");
    }
}
