using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Moq;
using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.Common.IntegrationEvents;
using Spamma.Modules.EmailInbox.Application.CommandHandlers.Email;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Email;
using Spamma.Modules.EmailInbox.Infrastructure.Services;

namespace Spamma.Modules.EmailInbox.Tests.Application.CommandHandlers.Email;

public class UpdateCatchAllModeCommandHandlerTests
{
    private readonly Mock<IEmailInboxSettingsService> _settingsServiceMock;
    private readonly Mock<IIntegrationEventPublisher> _eventPublisherMock;
    private readonly UpdateCatchAllModeCommandHandler _handler;

    public UpdateCatchAllModeCommandHandlerTests()
    {
        this._settingsServiceMock = new Mock<IEmailInboxSettingsService>(MockBehavior.Strict);
        this._eventPublisherMock = new Mock<IIntegrationEventPublisher>(MockBehavior.Strict);
        var loggerMock = new Mock<ILogger<UpdateCatchAllModeCommandHandler>>();
        var validators = Array.Empty<IValidator<UpdateCatchAllModeCommand>>();

        this._handler = new UpdateCatchAllModeCommandHandler(
            validators,
            loggerMock.Object,
            this._settingsServiceMock.Object,
            this._eventPublisherMock.Object);
    }

    [Fact]
    public async Task Handle_WhenEnablingCatchAll_SavesSettingsSuccessfully()
    {
        // Arrange
        this._settingsServiceMock
            .Setup(x => x.SetCatchAllModeEnabledAsync(true, CancellationToken.None))
            .Returns(Task.CompletedTask);
        this._eventPublisherMock
            .Setup(x => x.PublishAsync(
                It.Is<SystemSettingsUpdatedIntegrationEvent>(ev => ev.Settings.CatchAllModeEnabled),
                CancellationToken.None))
            .Returns(Task.CompletedTask);

        var command = new UpdateCatchAllModeCommand(true);

        // Act
        var result = await this._handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();
        this._settingsServiceMock.Verify(
            x => x.SetCatchAllModeEnabledAsync(true, CancellationToken.None),
            Times.Once);
        this._eventPublisherMock.Verify(
            x => x.PublishAsync(
                It.Is<SystemSettingsUpdatedIntegrationEvent>(ev => ev.Settings.CatchAllModeEnabled),
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenDisablingCatchAll_SavesSettingsSuccessfully()
    {
        // Arrange
        this._settingsServiceMock
            .Setup(x => x.SetCatchAllModeEnabledAsync(false, CancellationToken.None))
            .Returns(Task.CompletedTask);
        this._eventPublisherMock
            .Setup(x => x.PublishAsync(
                It.Is<SystemSettingsUpdatedIntegrationEvent>(ev => !ev.Settings.CatchAllModeEnabled),
                CancellationToken.None))
            .Returns(Task.CompletedTask);

        var command = new UpdateCatchAllModeCommand(false);

        // Act
        var result = await this._handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();
        this._settingsServiceMock.Verify(
            x => x.SetCatchAllModeEnabledAsync(false, CancellationToken.None),
            Times.Once);
        this._eventPublisherMock.Verify(
            x => x.PublishAsync(
                It.Is<SystemSettingsUpdatedIntegrationEvent>(ev => !ev.Settings.CatchAllModeEnabled),
                CancellationToken.None),
            Times.Once);
    }
}
