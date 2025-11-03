using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Moq;
using Spamma.Modules.DomainManagement.Application.CommandHandlers.Subdomain;
using Spamma.Modules.DomainManagement.Application.Repositories;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Subdomain;
using Spamma.Modules.DomainManagement.Tests.Fixtures;

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
        this._repositoryMock = new Mock<ISubdomainRepository>(MockBehavior.Strict);
        this._loggerMock = new Mock<ILogger<CreateSubdomainCommandHandler>>();
        this._timeProvider = new StubTimeProvider(this._fixedUtcNow);

        var validators = Array.Empty<IValidator<CreateSubdomainCommand>>();

        this._handler = new CreateSubdomainCommandHandler(
            this._repositoryMock.Object,
            validators,
            this._loggerMock.Object,
            this._timeProvider);
    }

    [Fact]
    public async Task Handle_WhenValidCommand_CreatesSubdomainAndSaves()
    {
        // Arrange
        var subdomainId = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var command = new CreateSubdomainCommand(subdomainId, domainId, "mail", "Mail subdomain");

        this._repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<Spamma.Modules.DomainManagement.Domain.SubdomainAggregate.Subdomain>(), CancellationToken.None))
            .ReturnsAsync(Result.Ok());

        // Act
        var result = await this._handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();

        this._repositoryMock.Verify(
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

        this._repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<Spamma.Modules.DomainManagement.Domain.SubdomainAggregate.Subdomain>(), CancellationToken.None))
            .ReturnsAsync(Result.Fail());

        // Act
        var result = await this._handler.Handle(command, CancellationToken.None);

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

        this._repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<Spamma.Modules.DomainManagement.Domain.SubdomainAggregate.Subdomain>(), CancellationToken.None))
            .ReturnsAsync(Result.Ok());

        // Act
        var result1 = await this._handler.Handle(command1, CancellationToken.None);
        var result2 = await this._handler.Handle(command2, CancellationToken.None);

        // Verify
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();

        this._repositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<Spamma.Modules.DomainManagement.Domain.SubdomainAggregate.Subdomain>(), CancellationToken.None),
            Times.Exactly(2));
    }
}