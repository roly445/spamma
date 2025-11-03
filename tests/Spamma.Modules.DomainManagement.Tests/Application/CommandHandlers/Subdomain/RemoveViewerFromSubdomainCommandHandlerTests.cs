using FluentAssertions;
using FluentValidation;
using MaybeMonad;
using Microsoft.Extensions.Logging;
using Moq;
using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.Common.IntegrationEvents.DomainManagement;
using Spamma.Modules.DomainManagement.Application.CommandHandlers.Subdomain;
using Spamma.Modules.DomainManagement.Application.Repositories;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Subdomain;
using Spamma.Modules.DomainManagement.Tests.Builders;
using Spamma.Modules.DomainManagement.Tests.Fixtures;

namespace Spamma.Modules.DomainManagement.Tests.Application.CommandHandlers.Subdomain;

public class RemoveViewerFromSubdomainCommandHandlerTests
{
    private readonly Mock<ISubdomainRepository> _repositoryMock;
    private readonly Mock<IIntegrationEventPublisher> _eventPublisherMock;
    private readonly Mock<ILogger<RemoveViewerFromSubdomainCommandHandler>> _loggerMock;
    private readonly RemoveViewerFromSubdomainCommandHandler _handler;
    private readonly TimeProvider _timeProvider;
    private readonly DateTime _fixedUtcNow = new(2024, 10, 15, 10, 30, 00, DateTimeKind.Utc);

    public RemoveViewerFromSubdomainCommandHandlerTests()
    {
        this._repositoryMock = new Mock<ISubdomainRepository>(MockBehavior.Strict);
        this._eventPublisherMock = new Mock<IIntegrationEventPublisher>(MockBehavior.Strict);
        this._loggerMock = new Mock<ILogger<RemoveViewerFromSubdomainCommandHandler>>();
        this._timeProvider = new StubTimeProvider(this._fixedUtcNow);

        var validators = Array.Empty<IValidator<RemoveViewerFromSubdomainCommand>>();

        this._handler = new RemoveViewerFromSubdomainCommandHandler(
            this._repositoryMock.Object,
            this._timeProvider,
            validators,
            this._loggerMock.Object,
            this._eventPublisherMock.Object);
    }

    [Fact]
    public async Task Handle_WhenSubdomainFound_RemoveViewerAndPublishesEvent()
    {
        // Arrange
        var subdomainId = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var subdomain = new SubdomainBuilder()
            .WithId(subdomainId)
            .WithDomainId(domainId)
            .WithName("mail")
            .WithViewer(userId, this._fixedUtcNow.AddSeconds(-10))
            .Build();

        var command = new RemoveViewerFromSubdomainCommand(subdomainId, userId);

        this._repositoryMock
            .Setup(x => x.GetByIdAsync(subdomainId, CancellationToken.None))
            .ReturnsAsync(Maybe.From(subdomain));

        this._repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<Spamma.Modules.DomainManagement.Domain.SubdomainAggregate.Subdomain>(), CancellationToken.None))
            .ReturnsAsync(Result.Ok());

        this._eventPublisherMock
            .Setup(x => x.PublishAsync(It.IsAny<UserRemovedFromBeingSubdomainViewerIntegrationEvent>(), CancellationToken.None))
            .Returns(Task.CompletedTask);

        // Act
        var result = await this._handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();

        this._repositoryMock.Verify(
            x => x.GetByIdAsync(subdomainId, CancellationToken.None),
            Times.Once);

        this._repositoryMock.Verify(
            x => x.SaveAsync(
                It.Is<Spamma.Modules.DomainManagement.Domain.SubdomainAggregate.Subdomain>(s => s.Id == subdomainId),
                CancellationToken.None),
            Times.Once);

        this._eventPublisherMock.Verify(
            x => x.PublishAsync(
                It.Is<UserRemovedFromBeingSubdomainViewerIntegrationEvent>(e =>
                    e.UserId == userId && e.SubdomainId == subdomainId),
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSubdomainNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var subdomainId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var command = new RemoveViewerFromSubdomainCommand(subdomainId, userId);

        this._repositoryMock
            .Setup(x => x.GetByIdAsync(subdomainId, CancellationToken.None))
            .ReturnsAsync(Maybe<Spamma.Modules.DomainManagement.Domain.SubdomainAggregate.Subdomain>.Nothing);

        // Act
        var result = await this._handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();

        this._repositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<Spamma.Modules.DomainManagement.Domain.SubdomainAggregate.Subdomain>(), CancellationToken.None),
            Times.Never);

        this._eventPublisherMock.Verify(
            x => x.PublishAsync(It.IsAny<UserRemovedFromBeingSubdomainViewerIntegrationEvent>(), CancellationToken.None),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenRepositorySaveFails_ReturnsError()
    {
        // Arrange
        var subdomainId = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var subdomain = new SubdomainBuilder()
            .WithId(subdomainId)
            .WithDomainId(domainId)
            .WithName("mail")
            .WithViewer(userId, this._fixedUtcNow.AddSeconds(-10))
            .Build();

        var command = new RemoveViewerFromSubdomainCommand(subdomainId, userId);

        this._repositoryMock
            .Setup(x => x.GetByIdAsync(subdomainId, CancellationToken.None))
            .ReturnsAsync(Maybe.From(subdomain));

        this._repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<Spamma.Modules.DomainManagement.Domain.SubdomainAggregate.Subdomain>(), CancellationToken.None))
            .ReturnsAsync(Result.Fail());

        // Act
        var result = await this._handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();

        this._repositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<Spamma.Modules.DomainManagement.Domain.SubdomainAggregate.Subdomain>(), CancellationToken.None),
            Times.Once);

        this._eventPublisherMock.Verify(
            x => x.PublishAsync(It.IsAny<UserRemovedFromBeingSubdomainViewerIntegrationEvent>(), CancellationToken.None),
            Times.Never);
    }
}