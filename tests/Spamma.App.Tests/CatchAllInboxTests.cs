using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Spamma.App.Client.Infrastructure.Contracts;
using Spamma.App.Client.Pages.Inbox;
using Xunit;
using BluQube.Queries;
using Spamma.Modules.EmailInbox.Client.Application.Queries;

namespace Spamma.App.Tests;

public class CatchAllInboxTests : BunitContext
{
    [Fact]
    public void Render_WhenCatchAllDisabled_ShowsDisabledMessage()
    {
        // Arrange
        Services.AddSingleton<IOptions<Settings>>(Options.Create(new Settings { CatchAllModeEnabled = false }));

        var querierMock = new Mock<IQuerier>();
        Services.AddSingleton(querierMock.Object);

        // Act
        var cut = Render<CatchAllInbox>();

        // Verify
        cut.Find("[data-testid='disabled-state']").Should().NotBeNull();
        cut.Markup.Should().Contain("Catch-All Mode is disabled");
    }

    [Fact]
    public async Task Render_WhenCatchAllEnabledAndNoEmails_ShowsEmptyState()
    {
        // Arrange
        Services.AddSingleton<IOptions<Settings>>(Options.Create(new Settings { CatchAllModeEnabled = true }));

        var querierMock = new Mock<IQuerier>();
        querierMock
            .Setup(x => x.Send(It.IsAny<GetCatchAllEmailsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(QueryResult<GetCatchAllEmailsQueryResult>.Succeeded(
                new GetCatchAllEmailsQueryResult([], 0)));
        Services.AddSingleton(querierMock.Object);

        // Act
        var cut = Render<CatchAllInbox>();
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Verify
        cut.Find("[data-testid='empty-state']").Should().NotBeNull();
        cut.Markup.Should().Contain("No catch-all emails yet");
    }

    [Fact]
    public async Task Render_WhenCatchAllEnabledWithEmails_GroupsByDomain()
    {
        // Arrange
        Services.AddSingleton<IOptions<Settings>>(Options.Create(new Settings { CatchAllModeEnabled = true }));

        var emails = new List<GetCatchAllEmailsQueryResult.EmailSummary>
        {
            new(Guid.NewGuid(), "Hello World", "user@example.com", DateTimeOffset.UtcNow, false),
        };

        var groups = new List<GetCatchAllEmailsQueryResult.DomainGroup>
        {
            new("example.com", emails),
        };

        var querierMock = new Mock<IQuerier>();
        querierMock
            .Setup(x => x.Send(It.IsAny<GetCatchAllEmailsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(QueryResult<GetCatchAllEmailsQueryResult>.Succeeded(
                new GetCatchAllEmailsQueryResult(groups, 1)));
        Services.AddSingleton(querierMock.Object);

        // Act
        var cut = Render<CatchAllInbox>();
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Verify
        cut.FindAll("[data-testid='domain-group']").Should().HaveCount(1);
        cut.Markup.Should().Contain("From: example.com");
        cut.Markup.Should().Contain("Hello World");
    }
}
