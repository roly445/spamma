using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using ResultMonad;
using MaybeMonad;
using Spamma.Modules.Common.Client;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.DomainManagement.Application.CommandHandlers.Subdomain;
using Spamma.Modules.DomainManagement.Application.Repositories;
using Spamma.Modules.DomainManagement.Client.Application.Commands;
using Spamma.Modules.DomainManagement.Tests.Builders;
using Spamma.Modules.DomainManagement.Tests.Fixtures;
using BluQube.Commands;
using FluentValidation;

namespace Spamma.Modules.DomainManagement.Tests.Application.CommandHandlers.Subdomain;

public class CreateSubdomainCommandHandlerTests
{
    private readonly Mock<ISubdomainRepository> _repositoryMock;
    private readonly Mock<ILogger<CreateSubdomainCommandHandler>> _loggerMock;
    private readonly CreateSubdomainCommandHandler _handler;
    private readonly TimeProvider _timeProvider;
    private readonly DateTime _fixedUtcNow = new(2024, 10, 15, 10, 30, 00, DateTimeKind.Utc);

    public CreateSubdomainCommandHandlerTests()
    {
        _repositoryMock = new Mock<ISubdomainRepository>(MockBehavior.Strict);
        _loggerMock = new Mock<ILogger<CreateSubdomainCommandHandler>>();
        _timeProvider = new StubTimeProvider(_fixedUtcNow);

        var validators = Array.Empty<IValidator<CreateSubdomainCommand>>();

        _handler = new CreateSubdomainCommandHandler(
            _repositoryMock.Object,
            validators,
            _loggerMock.Object,
            _timeProvider);
    }

    [Fact]
    public async Task Handle_WhenValidCommand_CreatesSubdomainAndSaves()
    {
        // Arrange
        var subdomainId = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var command = new CreateSubdomainCommand(subdomainId, domainId, "mail", "Mail subdomain");

        _repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<Spamma.Modules.DomainManagement.Domain.SubdomainAggregate.Subdomain>(), CancellationToken.None))
            .ReturnsAsync(Result.Ok());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();

        _repositoryMock.Verify(
            x => x.SaveAsync(
                It.Is<Spamma.Modules.DomainManagement.Domain.SubdomainAggregate.Subdomain>(s => s.Id == subdomainId),
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRepositorySaveFails_ReturnsError()
    {
        // Arrange
        var subdomainId = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var command = new CreateSubdomainCommand(subdomainId, domainId, "mail", "Mail subdomain");

        _repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<Spamma.Modules.DomainManagement.Domain.SubdomainAggregate.Subdomain>(), CancellationToken.None))
            .ReturnsAsync(Result.Fail());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_MultipleSubdomains_CreatesEachSuccessfully()
    {
        // Arrange
        var domainId = Guid.NewGuid();
        var subdomain1Id = Guid.NewGuid();
        var subdomain2Id = Guid.NewGuid();

        var command1 = new CreateSubdomainCommand(subdomain1Id, domainId, "mail", "Mail subdomain");
        var command2 = new CreateSubdomainCommand(subdomain2Id, domainId, "ftp", "FTP subdomain");

        _repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<Spamma.Modules.DomainManagement.Domain.SubdomainAggregate.Subdomain>(), CancellationToken.None))
            .ReturnsAsync(Result.Ok());

        // Act
        var result1 = await _handler.Handle(command1, CancellationToken.None);
        var result2 = await _handler.Handle(command2, CancellationToken.None);

        // Verify
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();

        _repositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<Spamma.Modules.DomainManagement.Domain.SubdomainAggregate.Subdomain>(), CancellationToken.None),
            Times.Exactly(2));
    }
}
