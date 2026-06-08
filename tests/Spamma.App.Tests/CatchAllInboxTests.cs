using System.IO.Compression;
using System.Text;
using BluQube.Commands;
using BluQube.Constants;
using BluQube.Queries;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Spamma.App.Client.Infrastructure.Constants;
using Spamma.App.Client.Infrastructure.Contracts.Services;
using Spamma.App.Client.Pages.Inbox;
using Spamma.Modules.Common.Client.Application.Queries;
using Spamma.Modules.EmailInbox.Client.Application.Queries;
using Xunit;

namespace Spamma.App.Tests;

public class CatchAllInboxTests : BunitContext
{
    [Fact]
    public void Render_WhenCatchAllDisabled_ShowsDisabledMessage()
    {
        // Arrange
        var querierMock = new Mock<IQueryRunner>(MockBehavior.Strict);
        Services.AddSingleton(querierMock.Object);
        Services.AddSingleton<ISystemSettingsCache>(new TestSystemSettingsCache(false));
        Services.AddSingleton<ISignalRService>(new TestSignalRService());
        Services.AddSingleton<IClientSessionContext>(new TestClientSessionContext());

        // Act
        var cut = Render<CatchAllInbox>();

        // Verify
        cut.Find("[data-testid='disabled-state']").Should().NotBeNull();
        cut.Markup.Should().Contain("Catch-All Mode is disabled");
        querierMock.Verify(
            x => x.Send(It.IsAny<GetSystemSettingsQuery>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Render_WhenCatchAllEnabledAndNoEmails_ShowsEmptyState()
    {
        // Arrange
        var querierMock = new Mock<IQueryRunner>();
        querierMock
            .Setup(x => x.Send(It.IsAny<GetCatchAllEmailsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(QueryResult<GetCatchAllEmailsQueryResult>.Succeeded(
                new GetCatchAllEmailsQueryResult([], 0)));
        Services.AddSingleton(querierMock.Object);
        Services.AddSingleton<ISystemSettingsCache>(new TestSystemSettingsCache(true));
        Services.AddSingleton<ISignalRService>(new TestSignalRService());
        Services.AddSingleton<IClientSessionContext>(new TestClientSessionContext());

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
        var emails = new List<GetCatchAllEmailsQueryResult.EmailSummary>
        {
            new(Guid.NewGuid(), "Hello World", "user@example.com", DateTimeOffset.UtcNow, false),
        };

        var groups = new List<GetCatchAllEmailsQueryResult.SenderGroup>
        {
            new("example.com", emails),
        };

        var querierMock = new Mock<IQueryRunner>();
        querierMock
            .Setup(x => x.Send(It.IsAny<GetCatchAllEmailsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(QueryResult<GetCatchAllEmailsQueryResult>.Succeeded(
                new GetCatchAllEmailsQueryResult(groups, 1)));
        Services.AddSingleton(querierMock.Object);
        Services.AddSingleton<ISystemSettingsCache>(new TestSystemSettingsCache(true));
        Services.AddSingleton<ISignalRService>(new TestSignalRService());
        Services.AddSingleton<IClientSessionContext>(new TestClientSessionContext());

        // Act
        var cut = Render<CatchAllInbox>();
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Verify
        cut.FindAll("[data-testid='domain-group']").Should().HaveCount(1);
        cut.Markup.Should().Contain("From: example.com");
        cut.Markup.Should().Contain("Hello World");
    }

    [Fact]
    public void SelectCampaignEmail_ShowsCampaignLinkInPreview()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var emails = new List<GetCatchAllEmailsQueryResult.EmailSummary>
        {
            new(emailId, "Campaign Subject", "user@example.com", DateTimeOffset.UtcNow, false, campaignId, "spring-launch"),
        };

        var querierMock = new Mock<IQueryRunner>();
        querierMock
            .Setup(x => x.Send(It.IsAny<GetCatchAllEmailsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(QueryResult<GetCatchAllEmailsQueryResult>.Succeeded(
                new GetCatchAllEmailsQueryResult([new("sender@example.com", emails)], 1)));

        Services.AddSingleton(querierMock.Object);
        Services.AddSingleton(new HttpClient(new TestMimeContentHandler(emailId, CreateCompressedMimeMessage("Campaign Subject")))
        {
            BaseAddress = new Uri("http://localhost/"),
        });
        Services.AddSingleton(new Mock<ICommandRunner>().Object);
        Services.AddSingleton<ISystemSettingsCache>(new TestSystemSettingsCache(true));
        Services.AddSingleton<ISignalRService>(new TestSignalRService());
        Services.AddSingleton<INotificationService>(new TestNotificationService());
        Services.AddSingleton<IClientSessionContext>(new TestClientSessionContext());

        // Act
        var cut = Render<CatchAllInbox>();
        cut.WaitForElement("[data-testid='catch-all-email-row']").Click();

        // Verify
        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("spring-launch");
            cut.Markup.Should().Contain($"/m/campaigns/{campaignId}");
            cut.Markup.Should().Contain("View Campaign");
        });
    }

    [Fact]
    public async Task SettingsChanged_WhenCatchAllEnabled_LoadsEmails()
    {
        // Arrange
        var settingsCache = new TestSystemSettingsCache(false);
        var querierMock = new Mock<IQueryRunner>();
        querierMock
            .Setup(x => x.Send(It.IsAny<GetCatchAllEmailsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(QueryResult<GetCatchAllEmailsQueryResult>.Succeeded(
                new GetCatchAllEmailsQueryResult([], 0)));
        Services.AddSingleton(querierMock.Object);
        Services.AddSingleton<ISystemSettingsCache>(settingsCache);
        Services.AddSingleton<ISignalRService>(new TestSignalRService());
        Services.AddSingleton<IClientSessionContext>(new TestClientSessionContext());
        var cut = Render<CatchAllInbox>();

        // Act
        await cut.InvokeAsync(() => settingsCache.ApplyAsync(new GetSystemSettingsQueryResult(true)));

        // Verify
        cut.Find("[data-testid='empty-state']").Should().NotBeNull();
        querierMock.Verify(
            x => x.Send(It.IsAny<GetCatchAllEmailsQuery>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SettingsChanged_WhenCatchAllDisabled_ClearsEmailState()
    {
        // Arrange
        var settingsCache = new TestSystemSettingsCache(true);
        var emails = new List<GetCatchAllEmailsQueryResult.EmailSummary>
        {
            new(Guid.NewGuid(), "Hello World", "user@example.com", DateTimeOffset.UtcNow, false),
        };

        var groups = new List<GetCatchAllEmailsQueryResult.SenderGroup>
        {
            new("example.com", emails),
        };

        var querierMock = new Mock<IQueryRunner>();
        querierMock
            .Setup(x => x.Send(It.IsAny<GetCatchAllEmailsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(QueryResult<GetCatchAllEmailsQueryResult>.Succeeded(
                new GetCatchAllEmailsQueryResult(groups, 1)));
        Services.AddSingleton(querierMock.Object);
        Services.AddSingleton<ISystemSettingsCache>(settingsCache);
        Services.AddSingleton<ISignalRService>(new TestSignalRService());
        Services.AddSingleton<IClientSessionContext>(new TestClientSessionContext());
        var cut = Render<CatchAllInbox>();
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Act
        await cut.InvokeAsync(() => settingsCache.ApplyAsync(new GetSystemSettingsQueryResult(false)));

        // Verify
        cut.Find("[data-testid='disabled-state']").Should().NotBeNull();
        cut.Markup.Should().NotContain("Hello World");
    }

    [Fact]
    public async Task CatchAllEmailReceived_WhenCatchAllEnabled_ReloadsEmails()
    {
        // Arrange
        var signalRService = new TestSignalRService();
        var querierMock = new Mock<IQueryRunner>();
        querierMock
            .SetupSequence(x => x.Send(It.IsAny<GetCatchAllEmailsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(QueryResult<GetCatchAllEmailsQueryResult>.Succeeded(
                new GetCatchAllEmailsQueryResult([], 0)))
            .ReturnsAsync(QueryResult<GetCatchAllEmailsQueryResult>.Succeeded(
                new GetCatchAllEmailsQueryResult(
                    [new("example.com", [new(Guid.NewGuid(), "New Email", "user@example.com", DateTimeOffset.UtcNow, false)])],
                    1)));

        Services.AddSingleton(querierMock.Object);
        Services.AddSingleton<ISystemSettingsCache>(new TestSystemSettingsCache(true));
        Services.AddSingleton<ISignalRService>(signalRService);
        Services.AddSingleton<IClientSessionContext>(new TestClientSessionContext());
        var cut = Render<CatchAllInbox>();
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Act
        await cut.InvokeAsync(signalRService.RaiseCatchAllEmailReceivedAsync);

        // Verify
        cut.Markup.Should().Contain("New Email");
        querierMock.Verify(
            x => x.Send(It.IsAny<GetCatchAllEmailsQuery>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task Search_WhenTypingSearchText_LoadsCatchAllEmailsWithSearchText()
    {
        // Arrange
        var querierMock = new Mock<IQueryRunner>();
        querierMock
            .SetupSequence(x => x.Send(It.IsAny<GetCatchAllEmailsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(QueryResult<GetCatchAllEmailsQueryResult>.Succeeded(
                new GetCatchAllEmailsQueryResult([], 0)))
            .ReturnsAsync(QueryResult<GetCatchAllEmailsQueryResult>.Succeeded(
                new GetCatchAllEmailsQueryResult([], 0)));

        Services.AddSingleton(querierMock.Object);
        Services.AddSingleton<ISystemSettingsCache>(new TestSystemSettingsCache(true));
        Services.AddSingleton<ISignalRService>(new TestSignalRService());
        Services.AddSingleton<IClientSessionContext>(new TestClientSessionContext());
        var cut = Render<CatchAllInbox>();
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Act
        var searchInput = cut.Find("input[placeholder='Search catch-all emails...']");
        await searchInput.InputAsync("github");
        await searchInput.KeyPressAsync("Enter");

        // Verify
        querierMock.Verify(
            x => x.Send(
                It.Is<GetCatchAllEmailsQuery>(query => query.SearchText == "github"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private sealed class TestSystemSettingsCache(bool catchAllModeEnabled) : ISystemSettingsCache
    {
        public event Func<GetSystemSettingsQueryResult, Task>? SettingsChanged;

        public GetSystemSettingsQueryResult Current { get; private set; } = new(catchAllModeEnabled);

        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public async Task ApplyAsync(GetSystemSettingsQueryResult settings)
        {
            this.Current = settings;

            if (this.SettingsChanged is not null)
            {
                await this.SettingsChanged.Invoke(settings);
            }
        }
    }

    private sealed class TestSignalRService : ISignalRService
    {
        event Func<Task>? ISignalRService.OnNewEmailReceived
        {
            add { }
            remove { }
        }

        public event Func<Task>? OnCatchAllEmailReceived;

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

        public Task RaiseCatchAllEmailReceivedAsync()
            => this.OnCatchAllEmailReceived?.Invoke() ?? Task.CompletedTask;
    }

    private sealed class TestClientSessionContext : IClientSessionContext
    {
        public string CurrentRoute => "/m/catch-all";

        public Task InitializeAsync() => Task.CompletedTask;

        public Task<string> GetSessionIdAsync() => Task.FromResult("test-session");

        public Task TrackBreadcrumbAsync(string name, Dictionary<string, string?>? data = null)
            => Task.CompletedTask;
    }

    private static byte[] CreateCompressedMimeMessage(string subject)
    {
        var rawMessage = $"""
                         From: Sender <sender@example.com>
                         To: User <user@example.com>
                         Subject: {subject}
                         Date: Tue, 1 Apr 2025 10:00:00 +0000
                         MIME-Version: 1.0
                         Content-Type: text/plain; charset=utf-8

                         Hello from a campaign catch-all email.
                         """;

        using var compressedStream = new MemoryStream();
        using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Compress))
        {
            gzipStream.Write(Encoding.UTF8.GetBytes(rawMessage));
        }

        return compressedStream.ToArray();
    }

    private sealed class TestMimeContentHandler(Guid emailId, byte[] content) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.RequestUri!.ToString().Should().Be($"http://localhost/api/email-inbox/emails/{emailId}/mime-content");

            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(content),
            });
        }
    }

    private sealed class TestNotificationService : INotificationService
    {
        public event Action? OnChange;

        public IReadOnlyList<Notification> Notifications => [];

        public void ShowSuccess(string message, int? durationMs = null)
            => this.OnChange?.Invoke();

        public void ShowInfo(string message, int? durationMs = null)
            => this.OnChange?.Invoke();

        public void ShowWarning(string message, int? durationMs = null)
            => this.OnChange?.Invoke();

        public void ShowError(string message, int? durationMs = null)
            => this.OnChange?.Invoke();

        public void Show(string message, NotificationType type, int? durationMs = null, bool autoHide = true)
            => this.OnChange?.Invoke();

        public void Remove(Guid notificationId)
            => this.OnChange?.Invoke();

        public void Clear()
            => this.OnChange?.Invoke();
    }
}
