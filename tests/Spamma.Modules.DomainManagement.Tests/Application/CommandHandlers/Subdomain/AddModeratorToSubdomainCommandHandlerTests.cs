using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using ResultMonad;
using MaybeMonad;
using Spamma.Modules.Common.Client;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.Common.IntegrationEvents.DomainManagement;
using Spamma.Modules.DomainManagement.Application.CommandHandlers.Subdomain;
using Spamma.Modules.DomainManagement.Application.Repositories;
using Spamma.Modules.DomainManagement.Client.Application.Commands;
using Spamma.Modules.DomainManagement.Tests.Builders;
using Spamma.Modules.DomainManagement.Tests.Fixtures;
using BluQube.Commands;
using FluentValidation;

namespace Spamma.Modules.DomainManagement.Tests.Application.CommandHandlers.Subdomain;

public class AddModeratorToSubdomainCommandHandlerTests
{
    private readonly Mock<ISubdomainRepository> _repositoryMock;
    private readonly Mock<IIntegrationEventPublisher> _eventPublisherMock;
    private readonly Mock<ILogger<AddModeratorToSubdomainCommandHandler>> _loggerMock;
    private readonly AddModeratorToSubdomainCommandHandler _handler;
    private readonly TimeProvider _timeProvider;
    private readonly DateTime _fixedUtcNow = new(2024, 10, 15, 10, 30, 00, DateTimeKind.Utc);

    public AddModeratorToSubdomainCommandHandlerTests()
    {
        _repositoryMock = new Mock<ISubdomainRepository>(MockBehavior.Strict);
        _eventPublisherMock = new Mock<IIntegrationEventPublisher>(MockBehavior.Strict);
        _loggerMock = new Mock<ILogger<AddModeratorToSubdomainCommandHandler>>();
        _timeProvider = new StubTimeProvider(_fixedUtcNow);

        var validators = Array.Empty<IValidator<AddModeratorToSubdomainCommand>>();

        _handler = new AddModeratorToSubdomainCommandHandler(
            _repositoryMock.Object,
            _timeProvider,
            validators,
            _loggerMock.Object,
            _eventPublisherMock.Object);
    }

    [Fact]
    public async Task Handle_WhenSubdomainFound_AddModeratorAndPublishesEvent()
    {
        // Arrange
        var subdomainId = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var subdomain = new SubdomainBuilder()
            .WithId(subdomainId)
            .WithDomainId(domainId)
            .WithName("mail")
            .Build();

        var command = new AddModeratorToSubdomainCommand(subdomainId, userId);

        _repositoryMock
            .Setup(x => x.GetByIdAsync(subdomainId, CancellationToken.None))
            .ReturnsAsync(Maybe.From(subdomain));

        _repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<Spamma.Modules.DomainManagement.Domain.SubdomainAggregate.Subdomain>(), CancellationToken.None))
            .ReturnsAsync(Result.Ok());

        _eventPublisherMock
            .Setup(x => x.PublishAsync(It.IsAny<UserAddedAsSubdomainModeratorIntegrationEvent>(), CancellationToken.None))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();

        _repositoryMock.Verify(
            x => x.GetByIdAsync(subdomainId, CancellationToken.None),
            Times.Once);

        _repositoryMock.Verify(
            x => x.SaveAsync(
                It.Is<Spamma.Modules.DomainManagement.Domain.SubdomainAggregate.Subdomain>(s => s.Id == subdomainId),
                CancellationToken.None),
            Times.Once);

        _eventPublisherMock.Verify(
            x => x.PublishAsync(
                It.Is<UserAddedAsSubdomainModeratorIntegrationEvent>(e =>
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
        var command = new AddModeratorToSubdomainCommand(subdomainId, userId);

        _repositoryMock
            .Setup(x => x.GetByIdAsync(subdomainId, CancellationToken.None))
            .ReturnsAsync(Maybe<Spamma.Modules.DomainManagement.Domain.SubdomainAggregate.Subdomain>.Nothing);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();

        _repositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<Spamma.Modules.DomainManagement.Domain.SubdomainAggregate.Subdomain>(), CancellationToken.None),
            Times.Never);

        _eventPublisherMock.Verify(
            x => x.PublishAsync(It.IsAny<UserAddedAsSubdomainModeratorIntegrationEvent>(), CancellationToken.None),
            Times.Never);
    }

    [Fact]
    public async Task Handle_AddingMultipleModerators_PublishesMultipleEvents()
    {
        // Arrange
        var subdomainId = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();

        var subdomain = new SubdomainBuilder()
            .WithId(subdomainId)
            .WithDomainId(domainId)
            .WithName("mail")
            .Build();

        var command1 = new AddModeratorToSubdomainCommand(subdomainId, userId1);
        var command2 = new AddModeratorToSubdomainCommand(subdomainId, userId2);

        _repositoryMock
            .Setup(x => x.GetByIdAsync(subdomainId, CancellationToken.None))
            .ReturnsAsync(Maybe.From(subdomain));

        _repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<Spamma.Modules.DomainManagement.Domain.SubdomainAggregate.Subdomain>(), CancellationToken.None))
            .ReturnsAsync(Result.Ok());

        _eventPublisherMock
            .Setup(x => x.PublishAsync(It.IsAny<UserAddedAsSubdomainModeratorIntegrationEvent>(), CancellationToken.None))
            .Returns(Task.CompletedTask);

        // Act
        var result1 = await _handler.Handle(command1, CancellationToken.None);
        var result2 = await _handler.Handle(command2, CancellationToken.None);

        // Verify
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();

        _eventPublisherMock.Verify(
            x => x.PublishAsync(It.IsAny<UserAddedAsSubdomainModeratorIntegrationEvent>(), CancellationToken.None),
            Times.Exactly(2));
    }
}
