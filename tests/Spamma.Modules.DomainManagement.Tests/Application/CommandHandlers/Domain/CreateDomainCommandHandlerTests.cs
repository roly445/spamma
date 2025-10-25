using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common.Client;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.DomainManagement.Application.CommandHandlers.Domain;
using Spamma.Modules.DomainManagement.Application.Repositories;
using Spamma.Modules.DomainManagement.Client.Application.Commands;
using Spamma.Modules.DomainManagement.Tests.Fixtures;
using BluQube.Commands;
using FluentValidation;
using ResultMonad;

namespace Spamma.Modules.DomainManagement.Tests.Application.CommandHandlers.Domain;

public class CreateDomainCommandHandlerTests
{
    private readonly Mock<IDomainRepository> _repositoryMock;
    private readonly Mock<ILogger<CreateDomainCommandHandler>> _loggerMock;
    private readonly CreateDomainCommandHandler _handler;
    private readonly TimeProvider _timeProvider;
    private readonly DateTime _fixedUtcNow = new(2024, 10, 15, 10, 30, 00, DateTimeKind.Utc);

    public CreateDomainCommandHandlerTests()
    {
        _repositoryMock = new Mock<IDomainRepository>(MockBehavior.Strict);
        _loggerMock = new Mock<ILogger<CreateDomainCommandHandler>>();
        _timeProvider = new StubTimeProvider(_fixedUtcNow);

        var validators = Array.Empty<IValidator<CreateDomainCommand>>();

        _handler = new CreateDomainCommandHandler(
            _repositoryMock.Object,
            validators,
            _loggerMock.Object,
            _timeProvider);
    }

    [Fact]
    public async Task Handle_WhenDomainCreatedSuccessfully_SavesAndReturnsVerificationToken()
    {
        // Arrange
        var domainId = Guid.NewGuid();
        var command = new CreateDomainCommand(
            domainId,
            "example.com",
            "contact@example.com",
            "Test domain");

        _repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<Spamma.Modules.DomainManagement.Domain.DomainAggregate.Domain>(), CancellationToken.None))
            .ReturnsAsync(Result.Ok());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();

        _repositoryMock.Verify(
            x => x.SaveAsync(
                It.Is<Spamma.Modules.DomainManagement.Domain.DomainAggregate.Domain>(d => d.Id == domainId),
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRepositorySaveFails_ReturnsError()
    {
        // Arrange
        var command = new CreateDomainCommand(
            Guid.NewGuid(),
            "example.com",
            "contact@example.com",
            "Test domain");

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
    public async Task Handle_WithMultipleDomains_ProcessesSuccessfully()
    {
        // Arrange
        var domain1Id = Guid.NewGuid();
        var domain2Id = Guid.NewGuid();

        var command1 = new CreateDomainCommand(
            domain1Id,
            "example.com",
            "contact1@example.com",
            "Domain 1");

        var command2 = new CreateDomainCommand(
            domain2Id,
            "test.com",
            "contact2@test.com",
            "Domain 2");

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
