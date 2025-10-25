using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using ResultMonad;
using MaybeMonad;
using Spamma.Modules.Common.Client;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.Common.IntegrationEvents.DomainManagement;
using Spamma.Modules.DomainManagement.Application.CommandHandlers.Domain;
using Spamma.Modules.DomainManagement.Application.Repositories;
using Spamma.Modules.DomainManagement.Client.Application.Commands;
using Spamma.Modules.DomainManagement.Tests.Builders;
using Spamma.Modules.DomainManagement.Tests.Fixtures;
using BluQube.Commands;
using FluentValidation;

namespace Spamma.Modules.DomainManagement.Tests.Application.CommandHandlers.Domain;

public class RemoveModeratorFromDomainCommandHandlerTests
{
    private readonly Mock<IDomainRepository> _repositoryMock;
    private readonly Mock<IIntegrationEventPublisher> _eventPublisherMock;
    private readonly Mock<ILogger<RemoveModeratorFromDomainCommandHandler>> _loggerMock;
    private readonly RemoveModeratorFromDomainCommandHandler _handler;
    private readonly TimeProvider _timeProvider;
    private readonly DateTime _fixedUtcNow = new(2024, 10, 15, 10, 30, 00, DateTimeKind.Utc);

    public RemoveModeratorFromDomainCommandHandlerTests()
    {
        _repositoryMock = new Mock<IDomainRepository>(MockBehavior.Strict);
        _eventPublisherMock = new Mock<IIntegrationEventPublisher>(MockBehavior.Strict);
        _loggerMock = new Mock<ILogger<RemoveModeratorFromDomainCommandHandler>>();
        _timeProvider = new StubTimeProvider(_fixedUtcNow);

        var validators = Array.Empty<IValidator<RemoveModeratorFromDomainCommand>>();

        _handler = new RemoveModeratorFromDomainCommandHandler(
            _repositoryMock.Object,
            _timeProvider,
            validators,
            _loggerMock.Object,
            _eventPublisherMock.Object);
    }

    [Fact]
    public async Task Handle_WhenDomainFound_RemoveModeratorAndPublishesEvent()
    {
        // Arrange
        var domainId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var domain = new DomainBuilder()
            .WithId(domainId)
            .WithName("example.com")
            .WithModerator(userId, _fixedUtcNow.AddSeconds(-10))
            .Build();

        var command = new RemoveModeratorFromDomainCommand(domainId, userId);

        _repositoryMock
            .Setup(x => x.GetByIdAsync(domainId, CancellationToken.None))
            .ReturnsAsync(Maybe.From(domain));

        _repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<Spamma.Modules.DomainManagement.Domain.DomainAggregate.Domain>(), CancellationToken.None))
            .ReturnsAsync(Result.Ok());

        _eventPublisherMock
            .Setup(x => x.PublishAsync(It.IsAny<UserRemovedFromBeingDomainModeratorIntegrationEvent>(), CancellationToken.None))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();

        _repositoryMock.Verify(
            x => x.GetByIdAsync(domainId, CancellationToken.None),
            Times.Once);

        _repositoryMock.Verify(
            x => x.SaveAsync(
                It.Is<Spamma.Modules.DomainManagement.Domain.DomainAggregate.Domain>(d => d.Id == domainId),
                CancellationToken.None),
            Times.Once);

        _eventPublisherMock.Verify(
            x => x.PublishAsync(
                It.Is<UserRemovedFromBeingDomainModeratorIntegrationEvent>(e =>
                    e.UserId == userId && e.DomainId == domainId),
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenDomainNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var domainId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var command = new RemoveModeratorFromDomainCommand(domainId, userId);

        _repositoryMock
            .Setup(x => x.GetByIdAsync(domainId, CancellationToken.None))
            .ReturnsAsync(Maybe<Spamma.Modules.DomainManagement.Domain.DomainAggregate.Domain>.Nothing);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();

        _repositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<Spamma.Modules.DomainManagement.Domain.DomainAggregate.Domain>(), CancellationToken.None),
            Times.Never);

        _eventPublisherMock.Verify(
            x => x.PublishAsync(It.IsAny<UserRemovedFromBeingDomainModeratorIntegrationEvent>(), CancellationToken.None),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenRepositorySaveFails_ReturnsError()
    {
        // Arrange
        var domainId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var domain = new DomainBuilder()
            .WithId(domainId)
            .WithName("example.com")
            .WithModerator(userId, _fixedUtcNow.AddSeconds(-10))
            .Build();

        var command = new RemoveModeratorFromDomainCommand(domainId, userId);

        _repositoryMock
            .Setup(x => x.GetByIdAsync(domainId, CancellationToken.None))
            .ReturnsAsync(Maybe.From(domain));

        _repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<Spamma.Modules.DomainManagement.Domain.DomainAggregate.Domain>(), CancellationToken.None))
            .ReturnsAsync(Result.Fail());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();

        _repositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<Spamma.Modules.DomainManagement.Domain.DomainAggregate.Domain>(), CancellationToken.None),
            Times.Once);

        _eventPublisherMock.Verify(
            x => x.PublishAsync(It.IsAny<UserRemovedFromBeingDomainModeratorIntegrationEvent>(), CancellationToken.None),
            Times.Never);
    }

    [Fact]
    public async Task Handle_RemovingMultipleModerators_PublishesMultipleEvents()
    {
        // Arrange
        var domainId = Guid.NewGuid();
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();

        var domain = new DomainBuilder()
            .WithId(domainId)
            .WithName("example.com")
            .WithModerator(userId1, _fixedUtcNow.AddSeconds(-20))
            .WithModerator(userId2, _fixedUtcNow.AddSeconds(-10))
            .Build();

        var command1 = new RemoveModeratorFromDomainCommand(domainId, userId1);
        var command2 = new RemoveModeratorFromDomainCommand(domainId, userId2);

        _repositoryMock
            .Setup(x => x.GetByIdAsync(domainId, CancellationToken.None))
            .ReturnsAsync(Maybe.From(domain));

        _repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<Spamma.Modules.DomainManagement.Domain.DomainAggregate.Domain>(), CancellationToken.None))
            .ReturnsAsync(Result.Ok());

        _eventPublisherMock
            .Setup(x => x.PublishAsync(It.IsAny<UserRemovedFromBeingDomainModeratorIntegrationEvent>(), CancellationToken.None))
            .Returns(Task.CompletedTask);

        // Act
        var result1 = await _handler.Handle(command1, CancellationToken.None);
        var result2 = await _handler.Handle(command2, CancellationToken.None);

        // Verify
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();

        _eventPublisherMock.Verify(
            x => x.PublishAsync(It.IsAny<UserRemovedFromBeingDomainModeratorIntegrationEvent>(), CancellationToken.None),
            Times.Exactly(2));
    }
}
