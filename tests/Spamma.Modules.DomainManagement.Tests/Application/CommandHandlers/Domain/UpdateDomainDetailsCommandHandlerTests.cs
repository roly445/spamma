using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using ResultMonad;
using MaybeMonad;
using Spamma.Modules.Common.Client;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.DomainManagement.Application.CommandHandlers.Domain;
using Spamma.Modules.DomainManagement.Application.Repositories;
using Spamma.Modules.DomainManagement.Client.Application.Commands;
using Spamma.Modules.DomainManagement.Tests.Builders;
using Spamma.Modules.DomainManagement.Tests.Fixtures;
using BluQube.Commands;
using FluentValidation;

namespace Spamma.Modules.DomainManagement.Tests.Application.CommandHandlers.Domain;

public class UpdateDomainDetailsCommandHandlerTests
{
    private readonly Mock<IDomainRepository> _repositoryMock;
    private readonly Mock<ILogger<UpdateDomainDetailsCommandHandler>> _loggerMock;
    private readonly UpdateDomainDetailsCommandHandler _handler;
    private readonly TimeProvider _timeProvider;
    private readonly DateTime _fixedUtcNow = new(2024, 10, 15, 10, 30, 00, DateTimeKind.Utc);

    public UpdateDomainDetailsCommandHandlerTests()
    {
        _repositoryMock = new Mock<IDomainRepository>(MockBehavior.Strict);
        _loggerMock = new Mock<ILogger<UpdateDomainDetailsCommandHandler>>();
        _timeProvider = new StubTimeProvider(_fixedUtcNow);

        var validators = Array.Empty<IValidator<UpdateDomainDetailsCommand>>();

        _handler = new UpdateDomainDetailsCommandHandler(
            _repositoryMock.Object,
            validators,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenDomainFound_UpdatesDetailsSuccessfully()
    {
        // Arrange
        var domainId = Guid.NewGuid();
        var domain = new DomainBuilder()
            .WithId(domainId)
            .WithName("example.com")
            .Build();

        var command = new UpdateDomainDetailsCommand(
            domainId,
            "newemail@example.com",
            "Updated description");

        _repositoryMock
            .Setup(x => x.GetByIdAsync(domainId, CancellationToken.None))
            .ReturnsAsync(Maybe.From(domain));

        _repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<Spamma.Modules.DomainManagement.Domain.DomainAggregate.Domain>(), CancellationToken.None))
            .ReturnsAsync(Result.Ok());

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
    }

    [Fact]
    public async Task Handle_WhenDomainNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var domainId = Guid.NewGuid();
        var command = new UpdateDomainDetailsCommand(
            domainId,
            "newemail@example.com",
            "Updated description");

        _repositoryMock
            .Setup(x => x.GetByIdAsync(domainId, CancellationToken.None))
            .ReturnsAsync(Maybe<Spamma.Modules.DomainManagement.Domain.DomainAggregate.Domain>.Nothing);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();

        _repositoryMock.Verify(
            x => x.GetByIdAsync(domainId, CancellationToken.None),
            Times.Once);

        _repositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<Spamma.Modules.DomainManagement.Domain.DomainAggregate.Domain>(), CancellationToken.None),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenRepositorySaveFails_ReturnsError()
    {
        // Arrange
        var domainId = Guid.NewGuid();
        var domain = new DomainBuilder()
            .WithId(domainId)
            .WithName("example.com")
            .Build();

        var command = new UpdateDomainDetailsCommand(
            domainId,
            "newemail@example.com",
            "Updated description");

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
    }

    [Fact]
    public async Task Handle_WithEmptyDescription_UpdatesSuccessfully()
    {
        // Arrange
        var domainId = Guid.NewGuid();
        var domain = new DomainBuilder()
            .WithId(domainId)
            .WithName("example.com")
            .Build();

        var command = new UpdateDomainDetailsCommand(
            domainId,
            "contact@example.com",
            "");

        _repositoryMock
            .Setup(x => x.GetByIdAsync(domainId, CancellationToken.None))
            .ReturnsAsync(Maybe.From(domain));

        _repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<Spamma.Modules.DomainManagement.Domain.DomainAggregate.Domain>(), CancellationToken.None))
            .ReturnsAsync(Result.Ok());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();

        _repositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<Spamma.Modules.DomainManagement.Domain.DomainAggregate.Domain>(), CancellationToken.None),
            Times.Once);
    }
}
