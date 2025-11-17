using FluentAssertions;
using Spamma.Modules.EmailInbox.Client.Application.Queries;

namespace Spamma.Modules.EmailInbox.Tests.Integration.QueryProcessors;

public class GetCampaignsQueryProcessorTests : QueryProcessorIntegrationTestBase
{
    [Fact]
    public async Task Handle_WithMatchingSubdomain_ReturnsCampaigns()
    {
        // Arrange
        var subdomainId = Guid.NewGuid();
        await this.CreateCampaignsAsync(subdomainId, count: 3);
        await this.Session.SaveChangesAsync();

        var query = new GetCampaignsQuery(subdomainId, Page: 1, PageSize: 10);

        // Act
        var result = await this.Sender.Send(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().HaveCount(3);
        result.Data.TotalCount.Should().Be(3);
        result.Data.TotalPages.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WithNoMatchingSubdomain_ReturnsEmptyList()
    {
        // Arrange
        var subdomainId = Guid.NewGuid();
        var otherSubdomainId = Guid.NewGuid();
        await this.CreateCampaignsAsync(otherSubdomainId, count: 3);
        await this.Session.SaveChangesAsync();

        var query = new GetCampaignsQuery(subdomainId, Page: 1, PageSize: 10);

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
    public async Task Handle_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var subdomainId = Guid.NewGuid();
        await this.CreateCampaignsAsync(subdomainId, count: 15);
        await this.Session.SaveChangesAsync();

        var query = new GetCampaignsQuery(subdomainId, Page: 2, PageSize: 5);

        // Act
        var result = await this.Sender.Send(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().HaveCount(5);
        result.Data.TotalCount.Should().Be(15);
        result.Data.TotalPages.Should().Be(3);
        result.Data.Page.Should().Be(2);
    }

    [Fact]
    public async Task Handle_SortByCampaignValue_Ascending_ReturnsSortedResults()
    {
        // Arrange
        var subdomainId = Guid.NewGuid();
        await this.CreateCampaignsAsync(subdomainId, count: 5);
        await this.Session.SaveChangesAsync();

        var query = new GetCampaignsQuery(subdomainId, Page: 1, PageSize: 10, SortBy: "CampaignValue", SortDescending: false);

        // Act
        var result = await this.Sender.Send(query, CancellationToken.None);

        // Assert
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().HaveCount(5);
        result.Data.Items.Should().BeInAscendingOrder(c => c.CampaignValue);
    }

    [Fact]
    public async Task Handle_SortByLastReceivedAt_Descending_ReturnsSortedResults()
    {
        // Arrange
        var subdomainId = Guid.NewGuid();
        await this.CreateCampaignsAsync(subdomainId, count: 5);
        await this.Session.SaveChangesAsync();

        var query = new GetCampaignsQuery(subdomainId, Page: 1, PageSize: 10, SortBy: "LastReceivedAt", SortDescending: true);

        // Act
        var result = await this.Sender.Send(query, CancellationToken.None);

        // Assert
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().HaveCount(5);
        result.Data.Items.Should().BeInDescendingOrder(c => c.LastReceivedAt);
    }

    [Fact]
    public async Task Handle_SortByTotalCaptured_Descending_ReturnsSortedResults()
    {
        // Arrange
        var subdomainId = Guid.NewGuid();
        await this.CreateCampaignsAsync(subdomainId, count: 5);
        await this.Session.SaveChangesAsync();

        var query = new GetCampaignsQuery(subdomainId, Page: 1, PageSize: 10, SortBy: "TotalCaptured", SortDescending: true);

        // Act
        var result = await this.Sender.Send(query, CancellationToken.None);

        // Assert
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().HaveCount(5);
        result.Data.Items.Should().BeInDescendingOrder(c => c.TotalCaptured);
    }
}