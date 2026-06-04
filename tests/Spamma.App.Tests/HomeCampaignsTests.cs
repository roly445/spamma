using BluQube.Commands;
using BluQube.Constants;
using BluQube.Queries;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Spamma.App.Client.Infrastructure.Constants;
using Spamma.App.Client.Infrastructure.Contracts.Services;
using Spamma.App.Client.Pages;
using Spamma.Modules.Common.Client.Application.Queries;
using Spamma.Modules.DomainManagement.Client.Application.Queries;
using Spamma.Modules.DomainManagement.Client.Contracts;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Campaign;
using Spamma.Modules.EmailInbox.Client.Application.Queries;
using Xunit;

namespace Spamma.App.Tests;

public class HomeCampaignsTests : BunitContext
{
    private readonly Guid subdomainId = Guid.NewGuid();
    private readonly Guid campaignId = Guid.NewGuid();

    [Fact]
    public void Render_WhenCampaignRoute_ShowsCampaignTabAsActive()
    {
        var querierMock = this.CreateCampaignQueryMock([]);
        this.RegisterServices(querierMock);
        this.NavigateTo("/m/campaigns");

        var cut = Render<Home>();

        cut.Find("[data-testid='campaigns-home']").Should().NotBeNull();
        cut.Find("[data-testid='campaigns-tab']").GetAttribute("aria-current").Should().Be("page");
    }

    [Fact]
    public void Render_WhenCampaignRoute_LoadsCampaignList()
    {
        var campaign = this.CreateCampaign("spring-launch");
        var querierMock = this.CreateCampaignQueryMock([campaign]);
        this.RegisterServices(querierMock);
        this.NavigateTo("/m/campaigns");

        var cut = Render<Home>();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("spring-launch"));
        querierMock.Verify(
            x => x.Send(
                It.IsAny<GetCampaignsQuery>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void SelectCampaign_LoadsDetailInThirdColumn()
    {
        var campaign = this.CreateCampaign("detail-campaign");
        var detail = new GetCampaignDetailQueryResult(
            this.campaignId,
            "detail-campaign",
            DateTimeOffset.UtcNow.AddDays(-2),
            DateTimeOffset.UtcNow,
            12,
            [],
            null);

        var querierMock = this.CreateCampaignQueryMock([campaign]);
        querierMock
            .Setup(x => x.Send(
                It.Is<GetCampaignDetailQuery>(query =>
                    query.CampaignId == this.campaignId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(QueryResult<GetCampaignDetailQueryResult>.Succeeded(detail));

        this.RegisterServices(querierMock);
        this.NavigateTo("/m/campaigns");
        var cut = Render<Home>();

        cut.Find("[data-testid='campaign-row']").Click();

        cut.WaitForAssertion(() => cut.Find("[data-testid='campaign-detail']").TextContent.Should().Contain("detail-campaign"));
        Services.GetRequiredService<NavigationManager>().Uri.Should().EndWith($"/m/campaigns/{this.campaignId}");
        cut.Markup.Should().Contain("Total captured");
        cut.Markup.Should().Contain("No sample message available");
    }

    [Fact]
    public void DeleteCampaign_SendsCommandAndRefreshesList()
    {
        var campaign = this.CreateCampaign("delete-me");
        var querierMock = this.CreateCampaignQueryMock([campaign], []);
        var commanderMock = new Mock<ICommandRunner>();
        commanderMock
            .Setup(x => x.Send(
                It.Is<DeleteCampaignCommand>(command => command.CampaignId == this.campaignId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult.Succeeded());

        var notificationService = new TestNotificationService();
        this.RegisterServices(querierMock, commanderMock, notificationService);
        this.NavigateTo("/m/campaigns");
        var cut = Render<Home>();

        cut.Find("[data-testid='campaign-row'] button").Click();

        cut.WaitForAssertion(() => notificationService.SuccessMessages.Should().ContainSingle(message => message.Contains("delete-me", StringComparison.Ordinal)));
        commanderMock.Verify(
            x => x.Send(
                It.Is<DeleteCampaignCommand>(command => command.CampaignId == this.campaignId),
                It.IsAny<CancellationToken>()),
            Times.Once);
        querierMock.Verify(
            x => x.Send(It.IsAny<GetCampaignsQuery>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    private void RegisterServices(
        Mock<IQueryRunner> querierMock,
        Mock<ICommandRunner>? commanderMock = null,
        INotificationService? notificationService = null)
    {
        Services.AddSingleton(querierMock.Object);
        Services.AddSingleton((commanderMock ?? new Mock<ICommandRunner>()).Object);
        Services.AddSingleton<ISignalRService>(new TestSignalRService());
        Services.AddSingleton(notificationService ?? new TestNotificationService());
    }

    private void NavigateTo(string uri)
        => Services.GetRequiredService<NavigationManager>().NavigateTo(uri);

    private Mock<IQueryRunner> CreateCampaignQueryMock(
        IReadOnlyList<GetCampaignsQueryResult.CampaignSummary> campaigns,
        IReadOnlyList<GetCampaignsQueryResult.CampaignSummary>? refreshedCampaigns = null)
    {
        var querierMock = new Mock<IQueryRunner>();
        querierMock
            .Setup(x => x.Send(It.IsAny<SearchSubdomainsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(QueryResult<SearchSubdomainsQueryResult>.Succeeded(new SearchSubdomainsQueryResult(
                [new SearchSubdomainsQueryResult.SubdomainSummary(
                    this.subdomainId,
                    Guid.NewGuid(),
                    "app",
                    "example.com",
                    "app.example.com",
                    SubdomainStatus.Active,
                    DateTime.UtcNow,
                    0,
                    1,
                    null)],
                1,
                1,
                1000,
                1)));

        var firstResult = QueryResult<GetCampaignsQueryResult>.Succeeded(new GetCampaignsQueryResult(
            campaigns,
            campaigns.Count,
            1,
            50,
            campaigns.Count == 0 ? 0 : 1));

        if (refreshedCampaigns is null)
        {
            querierMock
                .Setup(x => x.Send(It.IsAny<GetCampaignsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(firstResult);
        }
        else
        {
            var secondResult = QueryResult<GetCampaignsQueryResult>.Succeeded(new GetCampaignsQueryResult(
                refreshedCampaigns,
                refreshedCampaigns.Count,
                1,
                50,
                refreshedCampaigns.Count == 0 ? 0 : 1));

            querierMock
                .SetupSequence(x => x.Send(It.IsAny<GetCampaignsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(firstResult)
                .ReturnsAsync(secondResult);
        }

        return querierMock;
    }

    private GetCampaignsQueryResult.CampaignSummary CreateCampaign(string campaignValue)
        => new(
            this.campaignId,
            Guid.NewGuid(),
            this.subdomainId,
            campaignValue,
            DateTimeOffset.UtcNow.AddDays(-2),
            DateTimeOffset.UtcNow,
            12);

    private sealed class TestSignalRService : ISignalRService
    {
        event Func<Task>? ISignalRService.OnNewEmailReceived
        {
            add { }
            remove { }
        }

        event Func<Task>? ISignalRService.OnCatchAllEmailReceived
        {
            add { }
            remove { }
        }

        event Func<Task>? ISignalRService.OnEmailDeleted
        {
            add { }
            remove { }
        }

        event Func<Task>? ISignalRService.OnEmailUpdated
        {
            add { }
            remove { }
        }

        event Func<Task>? ISignalRService.OnPermissionsUpdated
        {
            add { }
            remove { }
        }

        event Func<GetSystemSettingsQueryResult, Task>? ISignalRService.OnSystemSettingsUpdated
        {
            add { }
            remove { }
        }

        public bool IsConnected => true;

        public Task StartAsync() => Task.CompletedTask;

        public Task StopAsync() => Task.CompletedTask;
    }

    private sealed class TestNotificationService : INotificationService
    {
        public event Action? OnChange;

        public IReadOnlyList<Notification> Notifications => [];

        public List<string> SuccessMessages { get; } = [];

        public void ShowSuccess(string message, int? durationMs = null)
        {
            this.SuccessMessages.Add(message);
            this.OnChange?.Invoke();
        }

        public void ShowInfo(string message, int? durationMs = null)
        {
        }

        public void ShowWarning(string message, int? durationMs = null)
        {
        }

        public void ShowError(string message, int? durationMs = null)
        {
        }

        public void Show(string message, NotificationType type, int? durationMs = null, bool autoHide = true)
        {
        }

        public void Remove(Guid notificationId)
        {
        }

        public void Clear()
        {
        }
    }
}
