using FluentAssertions;
using Spamma.Modules.EmailInbox.Client.Application.Queries;
using Spamma.Modules.EmailInbox.Client.Contracts;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;
using Spamma.Modules.EmailInbox.Tests.Builders;

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

        var email1 = EmailLookupTestFactory.Create(
            id: Guid.NewGuid(),
            subdomainId: subdomainId1,
            domainId: domainId,
            subject: "Email for subdomain 1",
            sentAt: DateTime.UtcNow.AddDays(-1),
            isFavorite: false,
            emailAddresses: new List<EmailAddress>
            {
                new EmailAddress("to1@example.com", "To Name", EmailAddressType.To),
            });

        var email2 = EmailLookupTestFactory.Create(
            id: Guid.NewGuid(),
            subdomainId: subdomainId2,
            domainId: domainId,
            subject: "Email for subdomain 2",
            sentAt: DateTime.UtcNow.AddDays(-2),
            isFavorite: false,
            emailAddresses: new List<EmailAddress>
            {
                new EmailAddress("to2@example.com", "To Name", EmailAddressType.To),
            });

        this.Session.Store(email1);
        this.PersistEmailAddresses(email1);
        this.Session.Store(email2);
        this.PersistEmailAddresses(email2);
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

        var email1 = EmailLookupTestFactory.Create(
            id: Guid.NewGuid(),
            subdomainId: subdomainId,
            domainId: domainId,
            subject: "Important meeting notes",
            sentAt: DateTime.UtcNow.AddDays(-1),
            isFavorite: false,
            emailAddresses: new List<EmailAddress>
            {
                new EmailAddress("to@example.com", "To Name", EmailAddressType.To),
            });

        var email2 = EmailLookupTestFactory.Create(
            id: Guid.NewGuid(),
            subdomainId: subdomainId,
            domainId: domainId,
            subject: "Random message",
            sentAt: DateTime.UtcNow.AddDays(-2),
            isFavorite: false,
            emailAddresses: new List<EmailAddress>
            {
                new EmailAddress("to@example.com", "To Name", EmailAddressType.To),
            });

        this.Session.Store(email1);
        this.PersistEmailAddresses(email1);
        this.Session.Store(email2);
        this.PersistEmailAddresses(email2);
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

        var email1 = EmailLookupTestFactory.Create(
            id: Guid.NewGuid(),
            subdomainId: subdomainId,
            domainId: domainId,
            subject: "Email 1",
            sentAt: DateTime.UtcNow.AddDays(-1),
            isFavorite: false,
            emailAddresses: new List<EmailAddress>
            {
                new EmailAddress("john.doe@example.com", "John Doe", EmailAddressType.To),
            });

        var email2 = EmailLookupTestFactory.Create(
            id: Guid.NewGuid(),
            subdomainId: subdomainId,
            domainId: domainId,
            subject: "Email 2",
            sentAt: DateTime.UtcNow.AddDays(-2),
            isFavorite: false,
            emailAddresses: new List<EmailAddress>
            {
                new EmailAddress("jane.smith@example.com", "Jane Smith", EmailAddressType.To),
            });

        this.Session.Store(email1);
        this.PersistEmailAddresses(email1);
        this.Session.Store(email2);
        this.PersistEmailAddresses(email2);
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
            var email = EmailLookupTestFactory.Create(
                id: Guid.NewGuid(),
                subdomainId: subdomainId,
                domainId: domainId,
                subject: $"Email {i}",
                sentAt: DateTime.UtcNow.AddDays(-i),
                isFavorite: false,
                emailAddresses: new List<EmailAddress>
                {
                    new EmailAddress($"to{i}@example.com", "To Name", EmailAddressType.To),
                });

            this.Session.Store(email);
            this.PersistEmailAddresses(email);
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

        var email = EmailLookupTestFactory.Create(
            id: Guid.NewGuid(),
            subdomainId: subdomainId,
            domainId: domainId,
            subject: "Test Email",
            sentAt: DateTime.UtcNow,
            isFavorite: false,
            emailAddresses: new List<EmailAddress>
            {
                new EmailAddress("to@example.com", "To Name", EmailAddressType.To),
            });

        this.Session.Store(email);
        this.PersistEmailAddresses(email);
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

        var oldEmail = EmailLookupTestFactory.Create(
            id: Guid.NewGuid(),
            subdomainId: subdomainId,
            domainId: domainId,
            subject: "Old Email",
            sentAt: DateTime.UtcNow.AddDays(-10),
            isFavorite: false,
            emailAddresses: new List<EmailAddress>
            {
                new EmailAddress("old@example.com", "Old", EmailAddressType.To),
            });

        var newEmail = EmailLookupTestFactory.Create(
            id: Guid.NewGuid(),
            subdomainId: subdomainId,
            domainId: domainId,
            subject: "New Email",
            sentAt: DateTime.UtcNow.AddDays(-1),
            isFavorite: false,
            emailAddresses: new List<EmailAddress>
            {
                new EmailAddress("new@example.com", "New", EmailAddressType.To),
            });

        this.Session.Store(oldEmail);
        this.PersistEmailAddresses(oldEmail);
        this.Session.Store(newEmail);
        this.PersistEmailAddresses(newEmail);
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

        var activeEmail = EmailLookupTestFactory.Create(
            id: Guid.NewGuid(),
            subdomainId: subdomainId,
            domainId: domainId,
            subject: "Active",
            sentAt: DateTime.UtcNow.AddDays(-1),
            isFavorite: false,
            emailAddresses: new List<EmailAddress> { new("active@example.com", "Active", EmailAddressType.To) },
            deletedAt: null,
            campaignId: null);

        var deletedEmail = EmailLookupTestFactory.Create(
            id: Guid.NewGuid(),
            subdomainId: subdomainId,
            domainId: domainId,
            subject: "Deleted",
            sentAt: DateTime.UtcNow.AddDays(-2),
            isFavorite: false,
            emailAddresses: new List<EmailAddress> { new("deleted@example.com", "Deleted", EmailAddressType.To) },
            deletedAt: DateTime.UtcNow.AddHours(-1),
            campaignId: null);

        this.Session.Store(activeEmail);
        this.PersistEmailAddresses(activeEmail);
        this.Session.Store(deletedEmail);
        this.PersistEmailAddresses(deletedEmail);
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

        var email = EmailLookupTestFactory.Create(
            id: Guid.NewGuid(),
            subdomainId: subdomainId,
            domainId: domainId,
            subject: "IMPORTANT MESSAGE",
            sentAt: DateTime.UtcNow,
            isFavorite: false,
            emailAddresses: new List<EmailAddress>
            {
                new EmailAddress("to@example.com", "To Name", EmailAddressType.To),
            });

        this.Session.Store(email);
        this.PersistEmailAddresses(email);
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
