using FluentAssertions;
using Spamma.Modules.EmailInbox.Client.Application.Queries;
using Spamma.Modules.EmailInbox.Client.Contracts;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

namespace Spamma.Modules.EmailInbox.Tests.Integration.QueryProcessors;

public class SearchEmailsQueryProcessorTests : QueryProcessorIntegrationTestBase
{
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
            var email = new EmailLookup
            {
                Id = Guid.NewGuid(),
                SubdomainId = subdomainId,
                DomainId = domainId,
                Subject = $"Test Email {i}",
                SentAt = DateTime.UtcNow.AddHours(-i),
                IsFavorite = false,
                EmailAddresses = new List<EmailAddress>
                {
                    new($"sender{i}@example.com", "Sender", EmailAddressType.From),
                    new($"recipient{i}@example.com", "Recipient", EmailAddressType.To),
                },
            };

            this.Session.Store(email);
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
        var matchingEmail = new EmailLookup
        {
            Id = Guid.NewGuid(),
            SubdomainId = subdomainId,
            DomainId = domainId,
            Subject = "Important meeting notes",
            SentAt = DateTime.UtcNow,
            IsFavorite = false,
            EmailAddresses = new List<EmailAddress>
            {
                new("sender@example.com", "Sender", EmailAddressType.From),
                new("recipient@example.com", "Recipient", EmailAddressType.To),
            },
        };

        // Create email NOT matching search term
        var nonMatchingEmail = new EmailLookup
        {
            Id = Guid.NewGuid(),
            SubdomainId = subdomainId,
            DomainId = domainId,
            Subject = "Random topic",
            SentAt = DateTime.UtcNow.AddHours(-1),
            IsFavorite = false,
            EmailAddresses = new List<EmailAddress>
            {
                new("other@example.com", "Other", EmailAddressType.From),
                new("target@example.com", "Target", EmailAddressType.To),
            },
        };

        this.Session.Store(matchingEmail);
        this.Session.Store(nonMatchingEmail);
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
        var matchingEmail = new EmailLookup
        {
            Id = Guid.NewGuid(),
            SubdomainId = subdomainId,
            DomainId = domainId,
            Subject = "Test Subject",
            SentAt = DateTime.UtcNow,
            IsFavorite = false,
            EmailAddresses = new List<EmailAddress>
            {
                new("john.doe@example.com", "John Doe", EmailAddressType.From),
                new("recipient@example.com", "Recipient", EmailAddressType.To),
            },
        };

        // Create email with different addresses
        var nonMatchingEmail = new EmailLookup
        {
            Id = Guid.NewGuid(),
            SubdomainId = subdomainId,
            DomainId = domainId,
            Subject = "Other Subject",
            SentAt = DateTime.UtcNow.AddHours(-1),
            IsFavorite = false,
            EmailAddresses = new List<EmailAddress>
            {
                new("other@example.com", "Other", EmailAddressType.From),
                new("target@example.com", "Target", EmailAddressType.To),
            },
        };

        this.Session.Store(matchingEmail);
        this.Session.Store(nonMatchingEmail);
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
        var matchingEmail = new EmailLookup
        {
            Id = Guid.NewGuid(),
            SubdomainId = subdomainId,
            DomainId = domainId,
            Subject = "Test Subject",
            SentAt = DateTime.UtcNow,
            IsFavorite = false,
            EmailAddresses = new List<EmailAddress>
            {
                new("sender@example.com", "Alice Smith", EmailAddressType.From),
                new("recipient@example.com", "Recipient", EmailAddressType.To),
            },
        };

        // Create email with different name
        var nonMatchingEmail = new EmailLookup
        {
            Id = Guid.NewGuid(),
            SubdomainId = subdomainId,
            DomainId = domainId,
            Subject = "Other Subject",
            SentAt = DateTime.UtcNow.AddHours(-1),
            IsFavorite = false,
            EmailAddresses = new List<EmailAddress>
            {
                new("other@example.com", "Bob Jones", EmailAddressType.From),
                new("target@example.com", "Target", EmailAddressType.To),
            },
        };

        this.Session.Store(matchingEmail);
        this.Session.Store(nonMatchingEmail);
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
            var email = new EmailLookup
            {
                Id = Guid.NewGuid(),
                SubdomainId = subdomainId,
                DomainId = domainId,
                Subject = $"Email {i:D3}",
                SentAt = DateTime.UtcNow.AddMinutes(-i),
                IsFavorite = false,
                EmailAddresses = new List<EmailAddress>
                {
                    new($"sender{i}@example.com", "Sender", EmailAddressType.From),
                    new($"recipient{i}@example.com", "Recipient", EmailAddressType.To),
                },
            };

            this.Session.Store(email);
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
        var activeEmail = new EmailLookup
        {
            Id = Guid.NewGuid(),
            SubdomainId = subdomainId,
            DomainId = domainId,
            Subject = "Active Email",
            SentAt = DateTime.UtcNow,
            DeletedAt = null,
            IsFavorite = false,
            EmailAddresses = new List<EmailAddress>
            {
                new("sender@example.com", "Sender", EmailAddressType.From),
                new("recipient@example.com", "Recipient", EmailAddressType.To),
            },
        };

        // Create deleted email
        var deletedEmail = new EmailLookup
        {
            Id = Guid.NewGuid(),
            SubdomainId = subdomainId,
            DomainId = domainId,
            Subject = "Deleted Email",
            SentAt = DateTime.UtcNow.AddHours(-1),
            DeletedAt = DateTime.UtcNow.AddMinutes(-30),
            IsFavorite = false,
            EmailAddresses = new List<EmailAddress>
            {
                new("sender@example.com", "Sender", EmailAddressType.From),
                new("recipient@example.com", "Recipient", EmailAddressType.To),
            },
        };

        this.Session.Store(activeEmail);
        this.Session.Store(deletedEmail);
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
            var email = new EmailLookup
            {
                Id = Guid.NewGuid(),
                SubdomainId = subdomainId,
                DomainId = domainId,
                Subject = emailData.Subject,
                SentAt = emailData.WhenSent,
                IsFavorite = false,
                EmailAddresses = new List<EmailAddress>
                {
                    new("sender@example.com", "Sender", EmailAddressType.From),
                    new("recipient@example.com", "Recipient", EmailAddressType.To),
                },
            };

            this.Session.Store(email);
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