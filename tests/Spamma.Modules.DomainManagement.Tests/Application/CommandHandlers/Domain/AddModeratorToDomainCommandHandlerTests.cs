using FluentAssertions;
using FluentValidation;
using MaybeMonad;
using Microsoft.Extensions.Logging;
using Moq;
using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.Common.IntegrationEvents.DomainManagement;
using Spamma.Modules.DomainManagement.Application.CommandHandlers.Domain;
using Spamma.Modules.DomainManagement.Application.Repositories;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Domain;
using Spamma.Modules.DomainManagement.Tests.Builders;
using Spamma.Modules.DomainManagement.Tests.Fixtures;

namespace Spamma.Modules.DomainManagement.Tests.Application.CommandHandlers.Domain;

public class AddModeratorToDomainCommandHandlerTests
{
    private readonly Mock<IDomainRepository> _repositoryMock;
    private readonly Mock<IIntegrationEventPublisher> _eventPublisherMock;
    private readonly Mock<ILogger<AddModeratorToDomainCommandHandler>> _loggerMock;
    private readonly AddModeratorToDomainCommandHandler _handler;
    private readonly TimeProvider _timeProvider;
    private readonly DateTime _fixedUtcNow = new(2024, 10, 15, 10, 30, 00, DateTimeKind.Utc);

    public AddModeratorToDomainCommandHandlerTests()
    {
        this._repositoryMock = new Mock<IDomainRepository>(MockBehavior.Strict);
        this._eventPublisherMock = new Mock<IIntegrationEventPublisher>(MockBehavior.Strict);
        this._loggerMock = new Mock<ILogger<AddModeratorToDomainCommandHandler>>();
        this._timeProvider = new StubTimeProvider(this._fixedUtcNow);

        var validators = Array.Empty<IValidator<AddModeratorToDomainCommand>>();

        this._handler = new AddModeratorToDomainCommandHandler(
            this._repositoryMock.Object,
            this._timeProvider,
            validators,
            this._loggerMock.Object,
            this._eventPublisherMock.Object);
    }

    [Fact]
    public async Task Handle_WhenDomainFound_AddModeratorAndPublishesEvent()
    {
        // Arrange
        var domainId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var domain = new DomainBuilder()
            .WithId(domainId)
            .WithName("example.com")
            .Build();

        var command = new AddModeratorToDomainCommand(domainId, userId);

        this._repositoryMock
            .Setup(x => x.GetByIdAsync(domainId, CancellationToken.None))
            .ReturnsAsync(Maybe.From(domain));

        this._repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<Spamma.Modules.DomainManagement.Domain.DomainAggregate.Domain>(), CancellationToken.None))
            .ReturnsAsync(Result.Ok());

        this._eventPublisherMock
            .Setup(x => x.PublishAsync(It.IsAny<UserAddedAsDomainModeratorIntegrationEvent>(), CancellationToken.None))
            .Returns(Task.CompletedTask);

        // Act
        var result = await this._handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();

        this._repositoryMock.Verify(
            x => x.GetByIdAsync(domainId, CancellationToken.None),
            Times.Once);

        this._repositoryMock.Verify(
            x => x.SaveAsync(
                It.Is<Spamma.Modules.DomainManagement.Domain.DomainAggregate.Domain>(d => d.Id == domainId),
                CancellationToken.None),
            Times.Once);

        this._eventPublisherMock.Verify(
            x => x.PublishAsync(
                It.Is<UserAddedAsDomainModeratorIntegrationEvent>(e =>
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
        var command = new AddModeratorToDomainCommand(domainId, userId);

        this._repositoryMock
            .Setup(x => x.GetByIdAsync(domainId, CancellationToken.None))
            .ReturnsAsync(Maybe<Spamma.Modules.DomainManagement.Domain.DomainAggregate.Domain>.Nothing);

        // Act
        var result = await this._handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();

        this._repositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<Spamma.Modules.DomainManagement.Domain.DomainAggregate.Domain>(), CancellationToken.None),
            Times.Never);

        this._eventPublisherMock.Verify(
            x => x.PublishAsync(It.IsAny<UserAddedAsDomainModeratorIntegrationEvent>(), CancellationToken.None),
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
            .Build();

        var command = new AddModeratorToDomainCommand(domainId, userId);

        this._repositoryMock
            .Setup(x => x.GetByIdAsync(domainId, CancellationToken.None))
            .ReturnsAsync(Maybe.From(domain));

        this._repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<Spamma.Modules.DomainManagement.Domain.DomainAggregate.Domain>(), CancellationToken.None))
            .ReturnsAsync(Result.Fail());

        // Act
        var result = await this._handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();

        this._repositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<Spamma.Modules.DomainManagement.Domain.DomainAggregate.Domain>(), CancellationToken.None),
            Times.Once);

        this._eventPublisherMock.Verify(
            x => x.PublishAsync(It.IsAny<UserAddedAsDomainModeratorIntegrationEvent>(), CancellationToken.None),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithMultipleModerators_PublishesMultipleEvents()
    {
        // Arrange
        var domainId = Guid.NewGuid();
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();

        var domain = new DomainBuilder()
            .WithId(domainId)
            .WithName("example.com")
            .Build();

        var command1 = new AddModeratorToDomainCommand(domainId, userId1);
        var command2 = new AddModeratorToDomainCommand(domainId, userId2);

        this._repositoryMock
            .Setup(x => x.GetByIdAsync(domainId, CancellationToken.None))
            .ReturnsAsync(Maybe.From(domain));

        this._repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<Spamma.Modules.DomainManagement.Domain.DomainAggregate.Domain>(), CancellationToken.None))
            .ReturnsAsync(Result.Ok());

        this._eventPublisherMock
            .Setup(x => x.PublishAsync(It.IsAny<UserAddedAsDomainModeratorIntegrationEvent>(), CancellationToken.None))
            .Returns(Task.CompletedTask);

        // Act
        var result1 = await this._handler.Handle(command1, CancellationToken.None);
        var result2 = await this._handler.Handle(command2, CancellationToken.None);

        // Verify
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();

        this._eventPublisherMock.Verify(
            x => x.PublishAsync(It.IsAny<UserAddedAsDomainModeratorIntegrationEvent>(), CancellationToken.None),
            Times.Exactly(2));
    }
}