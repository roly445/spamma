using Moq;
using Spamma.App.Infrastructure.Contracts.Services;
using Spamma.App.Infrastructure.IntegrationEventSubscribers;
using Spamma.Modules.Common.Client.Application.Queries;
using Spamma.Modules.Common.IntegrationEvents;
using Xunit;

namespace Spamma.App.Tests;

public class NotifySubscribersWhenSystemSettingsAreUpdatedTests
{
    [Fact]
    public async Task Process_BroadcastsSystemSettings()
    {
        // Arrange
        var settings = new GetSystemSettingsQueryResult(true);
        var clientNotifierMock = new Mock<IClientNotifierService>(MockBehavior.Strict);
        clientNotifierMock
            .Setup(x => x.NotifySystemSettingsUpdated(settings))
            .Returns(Task.CompletedTask);
        var subscriber = new NotifySubscribersWhenSystemSettingsAreUpdated(clientNotifierMock.Object);

        // Act
        await subscriber.Process(new SystemSettingsUpdatedIntegrationEvent(settings));

        // Verify
        clientNotifierMock.Verify(x => x.NotifySystemSettingsUpdated(settings), Times.Once);
    }
}
