using FluentAssertions;
using Spamma.Modules.Common.Infrastructure;
using Spamma.Modules.EmailInbox.Domain.CampaignAggregate;
using Spamma.Modules.EmailInbox.Tests.Integration;

namespace Spamma.Modules.EmailInbox.Tests.Infrastructure.Repositories;

[Collection("PostgreSQL")]
public class CampaignRepositoryIntegrationTests : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _fixture;

    public CampaignRepositoryIntegrationTests(PostgreSqlFixture fixture)
    {
        this._fixture = fixture;
    }

    [Fact]
    public async Task SaveAsync_And_GetByIdAsync_NewCampaign_RoundtripSucceeds()
    {
        var repository = new GenericRepository<Campaign>(this._fixture.Session!);
        var campaignId = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;

        var createResult = Campaign.Create(campaignId, domainId, subdomainId, "test-campaign", messageId, createdAt.DateTime, createdAt);
        createResult.IsSuccess.Should().BeTrue();
        var campaign = createResult.Value;

        var saveResult = await repository.SaveAsync(campaign);
        await this._fixture.Session!.SaveChangesAsync();
        var retrievedMaybe = await repository.GetByIdAsync(campaignId);

        saveResult.IsSuccess.Should().BeTrue();
        retrievedMaybe.HasValue.Should().BeTrue();
        var retrieved = retrievedMaybe.Value;
        retrieved.Id.Should().Be(campaignId);
        retrieved.CampaignValue.Should().Be("test-campaign");
        retrieved.DeletedAt.Should().Be(DateTime.MinValue);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentCampaign_ReturnsNothing()
    {
        var repository = new GenericRepository<Campaign>(this._fixture.Session!);
        var nonExistentId = Guid.NewGuid();

        var retrievedMaybe = await repository.GetByIdAsync(nonExistentId);

        retrievedMaybe.HasValue.Should().BeFalse();
    }

    [Fact]
    public async Task SaveAsync_CampaignWithMultipleCaptures_PersistsAllMessageIds()
    {
        var repository = new GenericRepository<Campaign>(this._fixture.Session!);
        var campaignId = Guid.NewGuid();
        var messageId1 = Guid.NewGuid();
        var messageId2 = Guid.NewGuid();
        var messageId3 = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;

        var createResult = Campaign.Create(campaignId, Guid.NewGuid(), Guid.NewGuid(), "multi-capture", messageId1, createdAt.DateTime, createdAt);
        var campaign = createResult.Value;
        campaign.RecordCapture(messageId2, DateTimeOffset.UtcNow);
        campaign.RecordCapture(messageId3, DateTimeOffset.UtcNow);

        await repository.SaveAsync(campaign);
        await this._fixture.Session!.SaveChangesAsync();
        var retrievedMaybe = await repository.GetByIdAsync(campaignId);

        retrievedMaybe.HasValue.Should().BeTrue();
        var retrieved = retrievedMaybe.Value;

        // Campaign creation doesn't count as a capture, only explicit RecordCapture calls do
        retrieved.TotalCaptures.Should().Be(2); // Verify captures were recorded
    }

    [Fact]
    public async Task SaveAsync_DeletedCampaign_PersistsDeletedState()
    {
        var repository = new GenericRepository<Campaign>(this._fixture.Session!);
        var campaignId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;

        var createResult = Campaign.Create(campaignId, Guid.NewGuid(), Guid.NewGuid(), "to-delete", Guid.NewGuid(), createdAt.DateTime, createdAt);
        var campaign = createResult.Value;
        campaign.Delete(DateTimeOffset.UtcNow.DateTime);

        await repository.SaveAsync(campaign);
        await this._fixture.Session!.SaveChangesAsync();
        var retrievedMaybe = await repository.GetByIdAsync(campaignId);

        retrievedMaybe.HasValue.Should().BeTrue();
        retrievedMaybe.Value.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task SaveAsync_MultipleCampaigns_AllPersistIndependently()
    {
        var repository = new GenericRepository<Campaign>(this._fixture.Session!);
        var campaign1Id = Guid.NewGuid();
        var campaign2Id = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;

        var campaign1 = Campaign.Create(campaign1Id, Guid.NewGuid(), Guid.NewGuid(), "campaign-1", Guid.NewGuid(), createdAt.DateTime, createdAt).Value;
        var campaign2 = Campaign.Create(campaign2Id, Guid.NewGuid(), Guid.NewGuid(), "campaign-2", Guid.NewGuid(), createdAt.DateTime, createdAt).Value;

        await repository.SaveAsync(campaign1);
        await repository.SaveAsync(campaign2);
        await this._fixture.Session!.SaveChangesAsync();

        var retrieved1 = await repository.GetByIdAsync(campaign1Id);
        var retrieved2 = await repository.GetByIdAsync(campaign2Id);

        retrieved1.HasValue.Should().BeTrue();
        retrieved2.HasValue.Should().BeTrue();
        retrieved1.Value.CampaignValue.Should().Be("campaign-1");
        retrieved2.Value.CampaignValue.Should().Be("campaign-2");
    }

    [Fact]
    public async Task SaveAsync_CampaignEventSequence_MaintainsEventOrder()
    {
        var repository = new GenericRepository<Campaign>(this._fixture.Session!);
        var campaignId = Guid.NewGuid();
        var messageId1 = Guid.NewGuid();
        var messageId2 = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;

        var campaign = Campaign.Create(campaignId, Guid.NewGuid(), Guid.NewGuid(), "event-sequence", messageId1, createdAt.DateTime, createdAt).Value;
        campaign.RecordCapture(messageId2, DateTimeOffset.UtcNow);
        campaign.Delete(DateTimeOffset.UtcNow.DateTime);

        await repository.SaveAsync(campaign);
        await this._fixture.Session!.SaveChangesAsync();
        var retrieved = await repository.GetByIdAsync(campaignId);

        retrieved.HasValue.Should().BeTrue();
        retrieved.Value.IsDeleted.Should().BeTrue();

        // Campaign creation doesn't count as a capture, only explicit RecordCapture calls do
        retrieved.Value.TotalCaptures.Should().Be(1); // Verify capture was recorded
    }
}