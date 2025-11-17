using FluentAssertions;
using Spamma.Modules.EmailInbox.Client.Application.Queries;
using Spamma.Modules.EmailInbox.Client.Contracts;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

namespace Spamma.Modules.EmailInbox.Tests.Integration;

public class SearchEmailsQueryProcessorTests : QueryProcessorIntegrationTestBase
{
    [Fact]
    public async Task Handle_WithSubdomainClaim_ReturnsOnlyEmailsForThatSubdomain()
    {
        // Arrange
        var domainId = Guid.NewGuid();
        var subdomainId1 = Guid.NewGuid();
        var subdomainId2 = Guid.NewGuid();

        var email1 = new EmailLookup
        {
            Id = Guid.NewGuid(),
            Subject = "Email for subdomain 1",
            DomainId = domainId,
            SubdomainId = subdomainId1,
            SentAt = DateTime.UtcNow.AddDays(-1),
            EmailAddresses = new List<EmailAddress>
            {
                new EmailAddress("to1@example.com", "To Name", EmailAddressType.To),
            },
        };

        var email2 = new EmailLookup
        {
            Id = Guid.NewGuid(),
            Subject = "Email for subdomain 2",
            DomainId = domainId,
            SubdomainId = subdomainId2,
            SentAt = DateTime.UtcNow.AddDays(-2),
            EmailAddresses = new List<EmailAddress>
            {
                new EmailAddress("to2@example.com", "To Name", EmailAddressType.To),
            },
        };

        this.Session.Store(email1);
        this.Session.Store(email2);
        await this.Session.SaveChangesAsync();

        // Add claim for subdomain1 only
        this.HttpContextAccessor.AddSubdomainClaim(subdomainId1);

        var query = new SearchEmailsQuery();

        // Act
        var result = await this.Sender.Send(query);

        // Assert
        result.Data.Should().NotBeNull();
        result.Data.TotalCount.Should().Be(1);
        result.Data.Items.Should().HaveCount(1);
        result.Data.Items[0].EmailId.Should().Be(email1.Id);
        result.Data.Items[0].Subject.Should().Be("Email for subdomain 1");
    }

    [Fact]
    public async Task Handle_WithSearchText_FiltersEmailsBySubject()
    {
        // Arrange
        var domainId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();

        var email1 = new EmailLookup
        {
            Id = Guid.NewGuid(),
            Subject = "Important meeting notes",
            DomainId = domainId,
            SubdomainId = subdomainId,
            SentAt = DateTime.UtcNow.AddDays(-1),
            EmailAddresses = new List<EmailAddress>
            {
                new EmailAddress("to@example.com", "To Name", EmailAddressType.To),
            },
        };

        var email2 = new EmailLookup
        {
            Id = Guid.NewGuid(),
            Subject = "Random message",
            DomainId = domainId,
            SubdomainId = subdomainId,
            SentAt = DateTime.UtcNow.AddDays(-2),
            EmailAddresses = new List<EmailAddress>
            {
                new EmailAddress("to@example.com", "To Name", EmailAddressType.To),
            },
        };

        this.Session.Store(email1);
        this.Session.Store(email2);
        await this.Session.SaveChangesAsync();

        this.HttpContextAccessor.AddSubdomainClaim(subdomainId);

        var query = new SearchEmailsQuery(SearchText: "meeting");

        // Act
        var result = await this.Sender.Send(query);

        // Assert
        result.Data.Should().NotBeNull();
        result.Data.TotalCount.Should().Be(1);
        result.Data.Items.Should().HaveCount(1);
        result.Data.Items[0].Subject.Should().Be("Important meeting notes");
    }

    [Fact]
    public async Task Handle_WithSearchText_FiltersEmailsByEmailAddress()
    {
        // Arrange
        var domainId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();

        var email1 = new EmailLookup
        {
            Id = Guid.NewGuid(),
            Subject = "Email 1",
            DomainId = domainId,
            SubdomainId = subdomainId,
            SentAt = DateTime.UtcNow.AddDays(-1),
            EmailAddresses = new List<EmailAddress>
            {
                new EmailAddress("john.doe@example.com", "John Doe", EmailAddressType.To),
            },
        };

        var email2 = new EmailLookup
        {
            Id = Guid.NewGuid(),
            Subject = "Email 2",
            DomainId = domainId,
            SubdomainId = subdomainId,
            SentAt = DateTime.UtcNow.AddDays(-2),
            EmailAddresses = new List<EmailAddress>
            {
                new EmailAddress("jane.smith@example.com", "Jane Smith", EmailAddressType.To),
            },
        };

        this.Session.Store(email1);
        this.Session.Store(email2);
        await this.Session.SaveChangesAsync();

        this.HttpContextAccessor.AddSubdomainClaim(subdomainId);

        var query = new SearchEmailsQuery(SearchText: "john");

        // Act
        var result = await this.Sender.Send(query);

        // Assert
        result.Data.Should().NotBeNull();
        result.Data.TotalCount.Should().Be(1);
        result.Data.Items.Should().HaveCount(1);
        result.Data.Items[0].EmailId.Should().Be(email1.Id);
    }

    [Fact]
    public async Task Handle_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var domainId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();

        // Create 5 emails
        for (int i = 1; i <= 5; i++)
        {
            var email = new EmailLookup
            {
                Id = Guid.NewGuid(),
                Subject = $"Email {i}",
                DomainId = domainId,
                SubdomainId = subdomainId,
                SentAt = DateTime.UtcNow.AddDays(-i),
                EmailAddresses = new List<EmailAddress>
                {
                    new EmailAddress($"to{i}@example.com", "To Name", EmailAddressType.To),
                },
            };

            this.Session.Store(email);
        }

        await this.Session.SaveChangesAsync();

        this.HttpContextAccessor.AddSubdomainClaim(subdomainId);

        var query = new SearchEmailsQuery(Page: 2, PageSize: 2);

        // Act
        var result = await this.Sender.Send(query);

        // Assert
        result.Data.Should().NotBeNull();
        result.Data.TotalCount.Should().Be(5);
        result.Data.Items.Should().HaveCount(2);
        result.Data.Page.Should().Be(2);
        result.Data.PageSize.Should().Be(2);
        result.Data.TotalPages.Should().Be(3);
        result.Data.HasPreviousPage.Should().BeTrue();
        result.Data.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithNoSubdomainClaims_ReturnsEmptyResults()
    {
        // Arrange
        var domainId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();

        var email = new EmailLookup
        {
            Id = Guid.NewGuid(),
            Subject = "Test Email",
            DomainId = domainId,
            SubdomainId = subdomainId,
            SentAt = DateTime.UtcNow,
            EmailAddresses = new List<EmailAddress>
            {
                new EmailAddress("to@example.com", "To Name", EmailAddressType.To),
            },
        };

        this.Session.Store(email);
        await this.Session.SaveChangesAsync();

        // Don't add any subdomain claims
        var query = new SearchEmailsQuery();

        // Act
        var result = await this.Sender.Send(query);

        // Assert
        result.Data.Should().NotBeNull();
        result.Data.TotalCount.Should().Be(0);
        result.Data.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_OrdersByWhenSentDescending_NewestFirst()
    {
        // Arrange
        var domainId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();

        var oldEmail = new EmailLookup
        {
            Id = Guid.NewGuid(),
            Subject = "Old Email",
            DomainId = domainId,
            SubdomainId = subdomainId,
            SentAt = DateTime.UtcNow.AddDays(-10),
            EmailAddresses = new List<EmailAddress>
            {
                new EmailAddress("old@example.com", "Old", EmailAddressType.To),
            },
        };

        var newEmail = new EmailLookup
        {
            Id = Guid.NewGuid(),
            Subject = "New Email",
            DomainId = domainId,
            SubdomainId = subdomainId,
            SentAt = DateTime.UtcNow.AddDays(-1),
            EmailAddresses = new List<EmailAddress>
            {
                new EmailAddress("new@example.com", "New", EmailAddressType.To),
            },
        };

        this.Session.Store(oldEmail);
        this.Session.Store(newEmail);
        await this.Session.SaveChangesAsync();

        this.HttpContextAccessor.AddSubdomainClaim(subdomainId);

        var query = new SearchEmailsQuery();

        // Act
        var result = await this.Sender.Send(query);

        // Assert
        result.Data.Should().NotBeNull();
        result.Data.Items.Should().HaveCount(2);
        result.Data.Items[0].EmailId.Should().Be(newEmail.Id);  // Newest first
        result.Data.Items[1].EmailId.Should().Be(oldEmail.Id);  // Oldest last
    }

    [Fact]
    public async Task Handle_ExcludesDeletedEmails_OnlyReturnsNonDeleted()
    {
        // Arrange
        var domainId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();

        var activeEmail = new EmailLookup
        {
            Id = Guid.NewGuid(),
            Subject = "Active Email",
            DomainId = domainId,
            SubdomainId = subdomainId,
            SentAt = DateTime.UtcNow.AddDays(-1),
            DeletedAt = null,
            EmailAddresses = new List<EmailAddress>
            {
                new EmailAddress("active@example.com", "Active", EmailAddressType.To),
            },
        };

        var deletedEmail = new EmailLookup
        {
            Id = Guid.NewGuid(),
            Subject = "Deleted Email",
            DomainId = domainId,
            SubdomainId = subdomainId,
            SentAt = DateTime.UtcNow.AddDays(-2),
            DeletedAt = DateTime.UtcNow.AddHours(-1),
            EmailAddresses = new List<EmailAddress>
            {
                new EmailAddress("deleted@example.com", "Deleted", EmailAddressType.To),
            },
        };

        this.Session.Store(activeEmail);
        this.Session.Store(deletedEmail);
        await this.Session.SaveChangesAsync();

        this.HttpContextAccessor.AddSubdomainClaim(subdomainId);

        var query = new SearchEmailsQuery();

        // Act
        var result = await this.Sender.Send(query);

        // Assert
        result.Data.Should().NotBeNull();
        result.Data.TotalCount.Should().Be(1);
        result.Data.Items.Should().HaveCount(1);
        result.Data.Items[0].EmailId.Should().Be(activeEmail.Id);
    }

    [Fact]
    public async Task Handle_WithCaseInsensitiveSearch_FindsMatches()
    {
        // Arrange
        var domainId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();

        var email = new EmailLookup
        {
            Id = Guid.NewGuid(),
            Subject = "IMPORTANT MESSAGE",
            DomainId = domainId,
            SubdomainId = subdomainId,
            SentAt = DateTime.UtcNow,
            EmailAddresses = new List<EmailAddress>
            {
                new EmailAddress("to@example.com", "To Name", EmailAddressType.To),
            },
        };

        this.Session.Store(email);
        await this.Session.SaveChangesAsync();

        this.HttpContextAccessor.AddSubdomainClaim(subdomainId);

        var query = new SearchEmailsQuery(SearchText: "important");  // lowercase search

        // Act
        var result = await this.Sender.Send(query);

        // Assert
        result.Data.Should().NotBeNull();
        result.Data.TotalCount.Should().Be(1);
        result.Data.Items.Should().HaveCount(1);
        result.Data.Items[0].Subject.Should().Be("IMPORTANT MESSAGE");
    }
}
