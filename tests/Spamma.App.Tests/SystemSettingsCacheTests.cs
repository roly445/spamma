using BluQube.Queries;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Spamma.App.Client.Infrastructure.Services;
using Spamma.Modules.Common.Client.Application.Queries;
using Xunit;

namespace Spamma.App.Tests;

public class SystemSettingsCacheTests
{
    [Fact]
    public async Task InitializeAsync_FetchesSystemSettingsOnce()
    {
        // Arrange
        var querierMock = new Mock<IQueryRunner>(MockBehavior.Strict);
        querierMock
            .Setup(x => x.Send(It.IsAny<GetSystemSettingsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(QueryResult<GetSystemSettingsQueryResult>.Succeeded(new GetSystemSettingsQueryResult(true)));
        var loggerMock = new Mock<ILogger<SystemSettingsCache>>();
        var cache = new SystemSettingsCache(querierMock.Object, loggerMock.Object);

        // Act
        await cache.InitializeAsync();
        await cache.InitializeAsync();

        // Verify
        cache.Current.CatchAllModeEnabled.Should().BeTrue();
        querierMock.Verify(
            x => x.Send(It.IsAny<GetSystemSettingsQuery>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ApplyAsync_WhenSettingsChange_UpdatesCurrentAndRaisesEvent()
    {
        // Arrange
        var querierMock = new Mock<IQueryRunner>(MockBehavior.Strict);
        var loggerMock = new Mock<ILogger<SystemSettingsCache>>();
        var cache = new SystemSettingsCache(querierMock.Object, loggerMock.Object);
        GetSystemSettingsQueryResult? observedSettings = null;
        cache.SettingsChanged += settings =>
        {
            observedSettings = settings;
            return Task.CompletedTask;
        };

        // Act
        await cache.ApplyAsync(new GetSystemSettingsQueryResult(true));

        // Verify
        cache.Current.CatchAllModeEnabled.Should().BeTrue();
        observedSettings.Should().Be(new GetSystemSettingsQueryResult(true));
    }
}
