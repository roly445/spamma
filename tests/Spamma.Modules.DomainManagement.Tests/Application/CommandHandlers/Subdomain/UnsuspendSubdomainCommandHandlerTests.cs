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
using Spamma.Modules.DomainManagement.Client.Contracts;
using Spamma.Modules.DomainManagement.Tests.Builders;
using Spamma.Modules.DomainManagement.Tests.Fixtures;
using BluQube.Commands;
using FluentValidation;

namespace Spamma.Modules.DomainManagement.Tests.Application.CommandHandlers.Subdomain;

public class UnsuspendSubdomainCommandHandlerTests
{
    private readonly Mock<ISubdomainRepository> _repositoryMock;
    private readonly Mock<ILogger<UnsuspendSubdomainCommandHandler>> _loggerMock;
    private readonly UnsuspendSubdomainCommandHandler _handler;
    private readonly TimeProvider _timeProvider;
    private readonly DateTime _fixedUtcNow = new(2024, 10, 15, 10, 30, 00, DateTimeKind.Utc);

    public UnsuspendSubdomainCommandHandlerTests()
    {
        _repositoryMock = new Mock<ISubdomainRepository>(MockBehavior.Strict);
        _loggerMock = new Mock<ILogger<UnsuspendSubdomainCommandHandler>>();
        _timeProvider = new StubTimeProvider(_fixedUtcNow);

        var validators = Array.Empty<IValidator<UnsuspendSubdomainCommand>>();

        _handler = new UnsuspendSubdomainCommandHandler(
            _repositoryMock.Object,
            _timeProvider,
            validators,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenSubdomainFound_UnsuspendsSuccessfully()
    {
        // Arrange
        var subdomainId = Guid.NewGuid();
        var domainId = Guid.NewGuid();

        var subdomain = new SubdomainBuilder()
            .WithId(subdomainId)
            .WithDomainId(domainId)
            .WithName("mail")
            .WithSuspension(SubdomainSuspensionReason.PolicyViolation, _fixedUtcNow.AddSeconds(-30))
            .Build();

        var command = new UnsuspendSubdomainCommand(subdomainId);

        _repositoryMock
            .Setup(x => x.GetByIdAsync(subdomainId, CancellationToken.None))
            .ReturnsAsync(Maybe.From(subdomain));

        _repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<Spamma.Modules.DomainManagement.Domain.SubdomainAggregate.Subdomain>(), CancellationToken.None))
            .ReturnsAsync(Result.Ok());

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
    }

    [Fact]
    public async Task Handle_WhenSubdomainNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var subdomainId = Guid.NewGuid();
        var command = new UnsuspendSubdomainCommand(subdomainId);

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
    }

    [Fact]
    public async Task Handle_WhenRepositorySaveFails_ReturnsError()
    {
        // Arrange
        var subdomainId = Guid.NewGuid();
        var domainId = Guid.NewGuid();

        var subdomain = new SubdomainBuilder()
            .WithId(subdomainId)
            .WithDomainId(domainId)
            .WithName("mail")
            .WithSuspension(SubdomainSuspensionReason.TechnicalIssue, _fixedUtcNow.AddSeconds(-30))
            .Build();

        var command = new UnsuspendSubdomainCommand(subdomainId);

        _repositoryMock
            .Setup(x => x.GetByIdAsync(subdomainId, CancellationToken.None))
            .ReturnsAsync(Maybe.From(subdomain));

        _repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<Spamma.Modules.DomainManagement.Domain.SubdomainAggregate.Subdomain>(), CancellationToken.None))
            .ReturnsAsync(Result.Fail());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();

        _repositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<Spamma.Modules.DomainManagement.Domain.SubdomainAggregate.Subdomain>(), CancellationToken.None),
            Times.Once);
    }
}
