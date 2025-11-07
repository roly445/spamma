using BluQube.Queries;
using FluentAssertions;
using Spamma.Modules.EmailInbox.Application.QueryProcessors;
using Spamma.Modules.EmailInbox.Client.Application.Queries;
using Spamma.Modules.EmailInbox.Tests.Integration;

namespace Spamma.Modules.EmailInbox.Tests.Application.QueryProcessors;

public class GetCampaignsQueryProcessorTests : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _fixture;

    public GetCampaignsQueryProcessorTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Handle_WithoutCampaigns_ReturnsEmptyResult()
    {
        // Arrange
        var subdomainId = Guid.NewGuid();
        var query = new GetCampaignsQuery
        {
            SubdomainId = subdomainId,
            Page = 1,
            PageSize = 10,
            SortBy = "CampaignValue",
            SortDescending = false,
        };

        var processor = new GetCampaignsQueryProcessor(_fixture.Session!);

        // Act
        var result = await processor.Handle(query, CancellationToken.None);

        // Verify
        result.Data.Items.Should().BeEmpty();
        result.Data.TotalCount.Should().Be(0);
        result.Data.Page.Should().Be(1);
        result.Data.TotalPages.Should().Be(0);
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithCampaigns_ReturnsPaginatedResults()
    {
        // Arrange
        var subdomainId = Guid.NewGuid();
        var campaigns = await TestDataSeeder.CreateCampaignsAsync(_fixture.Session!, subdomainId, 5);

        var query = new GetCampaignsQuery
        {
            SubdomainId = subdomainId,
            Page = 1,
            PageSize = 10,
            SortBy = "CampaignValue",
            SortDescending = false,
        };

        var processor = new GetCampaignsQueryProcessor(_fixture.Session!);

        // Act
        var result = await processor.Handle(query, CancellationToken.None);

        // Verify
        result.Data.Items.Should().HaveCount(5);
        result.Data.TotalCount.Should().Be(5);
        result.Data.Page.Should().Be(1);
        result.Data.PageSize.Should().Be(10);
        result.Data.TotalPages.Should().Be(1);
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithPaging_ReturnCorrectPage()
    {
        // Arrange
        var subdomainId = Guid.NewGuid();
        var campaigns = await TestDataSeeder.CreateCampaignsAsync(_fixture.Session!, subdomainId, 15);

        var query = new GetCampaignsQuery
        {
            SubdomainId = subdomainId,
            Page = 2,
            PageSize = 5,
            SortBy = "CampaignValue",
            SortDescending = false,
        };

        var processor = new GetCampaignsQueryProcessor(_fixture.Session!);

        // Act
        var result = await processor.Handle(query, CancellationToken.None);

        // Verify
        result.Data.Items.Should().HaveCount(5);
        result.Data.TotalCount.Should().Be(15);
        result.Data.Page.Should().Be(2);
        result.Data.TotalPages.Should().Be(3);
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_FiltersBySubdomainId()
    {
        // Arrange
        var subdomainId1 = Guid.NewGuid();
        var subdomainId2 = Guid.NewGuid();

        await TestDataSeeder.CreateCampaignsAsync(_fixture.Session!, subdomainId1, 3);
        await TestDataSeeder.CreateCampaignsAsync(_fixture.Session!, subdomainId2, 4);

        var query = new GetCampaignsQuery
        {
            SubdomainId = subdomainId1,
            Page = 1,
            PageSize = 10,
            SortBy = "CampaignValue",
            SortDescending = false,
        };

        var processor = new GetCampaignsQueryProcessor(_fixture.Session!);

        // Act
        var result = await processor.Handle(query, CancellationToken.None);

        // Verify
        result.Data.Items.Should().HaveCount(3);
        result.Data.TotalCount.Should().Be(3);
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_SortsByTotalCaptured()
    {
        // Arrange
        var subdomainId = Guid.NewGuid();
        var campaigns = await TestDataSeeder.CreateCampaignsAsync(_fixture.Session!, subdomainId, 3);

        var query = new GetCampaignsQuery
        {
            SubdomainId = subdomainId,
            Page = 1,
            PageSize = 10,
            SortBy = "TotalCaptured",
            SortDescending = true,
        };

        var processor = new GetCampaignsQueryProcessor(_fixture.Session!);

        // Act
        var result = await processor.Handle(query, CancellationToken.None);

        // Verify
        result.Data.Items.Should().HaveCount(3);
        result.Data.Items[0].TotalCaptured.Should().BeGreaterThanOrEqualTo(result.Data.Items[1].TotalCaptured);
        result.IsSuccessful.Should().BeTrue();
    }
}
