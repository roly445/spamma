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

public class UnsuspendDomainCommandHandlerTests
{
    private readonly Mock<IDomainRepository> _repositoryMock;
    private readonly Mock<ILogger<UnsuspendDomainCommandHandler>> _loggerMock;
    private readonly UnsuspendDomainCommandHandler _handler;
    private readonly TimeProvider _timeProvider;
    private readonly DateTime _fixedUtcNow = new(2024, 10, 15, 10, 30, 00, DateTimeKind.Utc);

    public UnsuspendDomainCommandHandlerTests()
    {
        _repositoryMock = new Mock<IDomainRepository>(MockBehavior.Strict);
        _loggerMock = new Mock<ILogger<UnsuspendDomainCommandHandler>>();
        _timeProvider = new StubTimeProvider(_fixedUtcNow);

        var validators = Array.Empty<IValidator<UnsuspendDomainCommand>>();

        _handler = new UnsuspendDomainCommandHandler(
            _repositoryMock.Object,
            _timeProvider,
            validators,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenDomainFound_UnsuspendsSuccessfully()
    {
        // Arrange
        var domainId = Guid.NewGuid();
        var domain = new DomainBuilder()
            .WithId(domainId)
            .WithName("example.com")
            .WithSuspension(Spamma.Modules.DomainManagement.Client.Contracts.DomainSuspensionReason.PolicyViolation, _fixedUtcNow.AddSeconds(-30))
            .Build();

        var command = new UnsuspendDomainCommand(domainId);

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
        var command = new UnsuspendDomainCommand(domainId);

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
    }

    [Fact]
    public async Task Handle_WhenRepositorySaveFails_ReturnsError()
    {
        // Arrange
        var domainId = Guid.NewGuid();
        var domain = new DomainBuilder()
            .WithId(domainId)
            .WithName("example.com")
            .WithSuspension(Spamma.Modules.DomainManagement.Client.Contracts.DomainSuspensionReason.NonPayment, _fixedUtcNow.AddSeconds(-30))
            .Build();

        var command = new UnsuspendDomainCommand(domainId);

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
    public async Task Handle_MultipleUnsuspends_ProcessesSuccessfully()
    {
        // Arrange
        var domain1Id = Guid.NewGuid();
        var domain2Id = Guid.NewGuid();

        var domain1 = new DomainBuilder()
            .WithId(domain1Id)
            .WithName("example1.com")
            .WithSuspension(Spamma.Modules.DomainManagement.Client.Contracts.DomainSuspensionReason.PolicyViolation, _fixedUtcNow.AddSeconds(-60))
            .Build();

        var domain2 = new DomainBuilder()
            .WithId(domain2Id)
            .WithName("example2.com")
            .WithSuspension(Spamma.Modules.DomainManagement.Client.Contracts.DomainSuspensionReason.AbuseSpam, _fixedUtcNow.AddSeconds(-30))
            .Build();

        var command1 = new UnsuspendDomainCommand(domain1Id);
        var command2 = new UnsuspendDomainCommand(domain2Id);

        _repositoryMock
            .SetupSequence(x => x.GetByIdAsync(It.IsAny<Guid>(), CancellationToken.None))
            .ReturnsAsync(Maybe.From(domain1))
            .ReturnsAsync(Maybe.From(domain2));

        _repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<Spamma.Modules.DomainManagement.Domain.DomainAggregate.Domain>(), CancellationToken.None))
            .ReturnsAsync(Result.Ok());

        // Act
        var result1 = await _handler.Handle(command1, CancellationToken.None);
        var result2 = await _handler.Handle(command2, CancellationToken.None);

        // Verify
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();

        _repositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<Spamma.Modules.DomainManagement.Domain.DomainAggregate.Domain>(), CancellationToken.None),
            Times.Exactly(2));
    }
}
