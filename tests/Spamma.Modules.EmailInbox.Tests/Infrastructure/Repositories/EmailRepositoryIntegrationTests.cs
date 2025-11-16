using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Moq;
using Spamma.Modules.EmailInbox.Client.Contracts;
using Spamma.Modules.EmailInbox.Domain.EmailAggregate;
using Spamma.Modules.EmailInbox.Domain.EmailAggregate.Events;
using Spamma.Modules.EmailInbox.Infrastructure.Repositories;
using Spamma.Modules.EmailInbox.Tests.Integration;

namespace Spamma.Modules.EmailInbox.Tests.Infrastructure.Repositories;

[Collection("PostgreSQL")]
public class EmailRepositoryIntegrationTests : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _fixture;
    private readonly IHostEnvironment _hostEnvironment;

    public EmailRepositoryIntegrationTests(PostgreSqlFixture fixture)
    {
        this._fixture = fixture;
        this._hostEnvironment = Mock.Of<IHostEnvironment>(h => h.ContentRootPath == Path.GetTempPath());
    }

    [Fact]
    public async Task SaveAsync_And_GetByIdAsync_NewEmail_RoundtripSucceeds()
    {
        // Arrange
        var repository = new EmailRepository(this._fixture.Session!, this._hostEnvironment);
        var emailId = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();
        var addresses = new List<EmailReceived.EmailAddress>
        {
            new("test@example.com", "Test User", EmailAddressType.To),
        };

        var createResult = Email.Create(emailId, domainId, subdomainId, "Test Subject", DateTime.UtcNow, addresses);

        // Act - Save and retrieve
        createResult.IsSuccess.Should().BeTrue();
        var email = createResult.Value;

        var saveResult = await repository.SaveAsync(email);
        await this._fixture.Session!.SaveChangesAsync();

        var retrievedMaybe = await repository.GetByIdAsync(emailId);

        // Assert
        saveResult.IsSuccess.Should().BeTrue();
        retrievedMaybe.HasValue.Should().BeTrue();
        var retrieved = retrievedMaybe.Value;
        retrieved.Id.Should().Be(emailId);
        retrieved.Subject.Should().Be("Test Subject");
        retrieved.DomainId.Should().Be(domainId);
        retrieved.SubdomainId.Should().Be(subdomainId);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentEmail_ReturnsNothing()
    {
        // Arrange
        var repository = new EmailRepository(this._fixture.Session!, this._hostEnvironment);
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await repository.GetByIdAsync(nonExistentId);

        // Assert
        result.HasValue.Should().BeFalse();
    }

    [Fact]
    public async Task SaveAsync_EmailWithMultipleEvents_PersistsAllEvents()
    {
        // Arrange
        var repository = new EmailRepository(this._fixture.Session!, this._hostEnvironment);
        var emailId = Guid.NewGuid();
        var addresses = new List<EmailReceived.EmailAddress>
        {
            new("test@example.com", "Test", EmailAddressType.To),
        };

        var createResult = Email.Create(emailId, Guid.NewGuid(), Guid.NewGuid(), "Multi Event", DateTime.UtcNow, addresses);
        var email = createResult.Value;

        await repository.SaveAsync(email);
        await this._fixture.Session!.SaveChangesAsync();

        // Act - Generate more events
        var markResult = email.MarkAsFavorite(DateTime.UtcNow);
        markResult.IsSuccess.Should().BeTrue();

        await repository.SaveAsync(email);
        await this._fixture.Session!.SaveChangesAsync();

        // Assert - Verify all events applied
        var retrievedMaybe = await repository.GetByIdAsync(emailId);
        retrievedMaybe.HasValue.Should().BeTrue();
        var retrieved = retrievedMaybe.Value;
        retrieved.IsFavorite.Should().BeTrue();
    }

    [Fact]
    public async Task SaveAsync_MultipleEmails_AllPersistIndependently()
    {
        // Arrange
        var repository = new EmailRepository(this._fixture.Session!, this._hostEnvironment);
        var addresses = new List<EmailReceived.EmailAddress>
        {
            new("test@example.com", "Test", EmailAddressType.To),
        };

        var email1Id = Guid.NewGuid();
        var email2Id = Guid.NewGuid();
        var email3Id = Guid.NewGuid();

        var email1 = Email.Create(email1Id, Guid.NewGuid(), Guid.NewGuid(), "Email 1", DateTime.UtcNow, addresses).Value;
        var email2 = Email.Create(email2Id, Guid.NewGuid(), Guid.NewGuid(), "Email 2", DateTime.UtcNow, addresses).Value;
        var email3 = Email.Create(email3Id, Guid.NewGuid(), Guid.NewGuid(), "Email 3", DateTime.UtcNow, addresses).Value;

        // Act
        await repository.SaveAsync(email1);
        await repository.SaveAsync(email2);
        await repository.SaveAsync(email3);
        await this._fixture.Session!.SaveChangesAsync();

        // Assert - All emails retrievable
        var retrieved1 = await repository.GetByIdAsync(email1Id);
        var retrieved2 = await repository.GetByIdAsync(email2Id);
        var retrieved3 = await repository.GetByIdAsync(email3Id);

        retrieved1.HasValue.Should().BeTrue();
        retrieved1.Value.Subject.Should().Be("Email 1");

        retrieved2.HasValue.Should().BeTrue();
        retrieved2.Value.Subject.Should().Be("Email 2");

        retrieved3.HasValue.Should().BeTrue();
        retrieved3.Value.Subject.Should().Be("Email 3");
    }

    [Fact]
    public async Task SaveAsync_EmailEventSequence_MaintainsCorrectState()
    {
        // Arrange
        var repository = new EmailRepository(this._fixture.Session!, this._hostEnvironment);
        var emailId = Guid.NewGuid();
        var addresses = new List<EmailReceived.EmailAddress>
        {
            new("test@example.com", "Test", EmailAddressType.To),
        };

        var email = Email.Create(emailId, Guid.NewGuid(), Guid.NewGuid(), "Sequence", DateTime.UtcNow, addresses).Value;

        await repository.SaveAsync(email);
        await this._fixture.Session!.SaveChangesAsync();

        // Act - Multiple state changes
        email.MarkAsFavorite(DateTime.UtcNow);
        await repository.SaveAsync(email);
        await this._fixture.Session!.SaveChangesAsync();

        email.UnmarkAsFavorite(DateTime.UtcNow);
        await repository.SaveAsync(email);
        await this._fixture.Session!.SaveChangesAsync();

        email.MarkAsFavorite(DateTime.UtcNow);
        await repository.SaveAsync(email);
        await this._fixture.Session!.SaveChangesAsync();

        // Assert - Final state is favorite
        var retrievedMaybe = await repository.GetByIdAsync(emailId);
        retrievedMaybe.HasValue.Should().BeTrue();
        retrievedMaybe.Value.IsFavorite.Should().BeTrue();
    }

    [Fact]
    public async Task SaveAsync_DeletedEmail_PersistsDeletedState()
    {
        // Arrange
        var repository = new EmailRepository(this._fixture.Session!, this._hostEnvironment);
        var emailId = Guid.NewGuid();
        var addresses = new List<EmailReceived.EmailAddress>
        {
            new("test@example.com", "Test", EmailAddressType.To),
        };

        var email = Email.Create(emailId, Guid.NewGuid(), Guid.NewGuid(), "To Delete", DateTime.UtcNow, addresses).Value;

        await repository.SaveAsync(email);
        await this._fixture.Session!.SaveChangesAsync();

        // Act - Delete
        var deleteResult = email.Delete(DateTime.UtcNow);
        deleteResult.IsSuccess.Should().BeTrue();

        await repository.SaveAsync(email);
        await this._fixture.Session!.SaveChangesAsync();

        // Assert - Deleted state persisted
        var retrievedMaybe = await repository.GetByIdAsync(emailId);
        retrievedMaybe.HasValue.Should().BeTrue();
        retrievedMaybe.Value.WhenDeleted.HasValue.Should().BeTrue();
    }
}