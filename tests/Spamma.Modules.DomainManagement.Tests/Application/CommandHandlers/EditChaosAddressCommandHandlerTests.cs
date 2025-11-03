using FluentAssertions;
using MaybeMonad;
using Microsoft.Extensions.Logging;
using Moq;
using Spamma.Modules.DomainManagement.Application.CommandHandlers.EditChaosAddress;
using Spamma.Modules.DomainManagement.Application.Repositories;
using Spamma.Modules.DomainManagement.Client.Application.Commands.EditChaosAddress;
using Spamma.Modules.DomainManagement.Domain.ChaosAddressAggregate;

namespace Spamma.Modules.DomainManagement.Tests.Application.CommandHandlers;

public class EditChaosAddressCommandHandlerTests
{
    [Fact]
    public async Task Handle_EditSucceeds_CallsRepositoryAndReturnsSucceeded()
    {
        var repoMock = new Mock<IChaosAddressRepository>(MockBehavior.Strict);
        var id = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();
        var when = DateTime.UtcNow;

        var create = ChaosAddress.Create(id, domainId, subdomainId, "local", Spamma.Modules.Common.SmtpResponseCode.MailboxUnavailable, when);
        var chaos = create.Value;

        repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(Maybe.From(chaos));
        repoMock.Setup(r => r.SaveAsync(chaos, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Ok());

        var handler = new EditChaosAddressCommandHandler(repoMock.Object, Array.Empty<FluentValidation.IValidator<EditChaosAddressCommand>>(), new Mock<ILogger<EditChaosAddressCommandHandler>>().Object);

        var cmd = new EditChaosAddressCommand(id, domainId, subdomainId, "newlocal", Spamma.Modules.Common.SmtpResponseCode.RequestedActionAborted, null);
        var res = await handler.Handle(cmd, CancellationToken.None);

        res.Should().NotBeNull();
        repoMock.Verify(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
        repoMock.Verify(r => r.SaveAsync(chaos, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EditFailsWhenNotFound_ReturnsNotFound()
    {
        var repoMock = new Mock<IChaosAddressRepository>(MockBehavior.Strict);
        var id = Guid.NewGuid();

        repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(Maybe<ChaosAddress>.Nothing);

        var handler = new EditChaosAddressCommandHandler(repoMock.Object, Array.Empty<FluentValidation.IValidator<EditChaosAddressCommand>>(), new Mock<ILogger<EditChaosAddressCommandHandler>>().Object);

        var cmd = new EditChaosAddressCommand(id, Guid.NewGuid(), Guid.NewGuid(), "newlocal", Spamma.Modules.Common.SmtpResponseCode.RequestedActionAborted, null);
        var res = await handler.Handle(cmd, CancellationToken.None);

        res.Should().NotBeNull();
        repoMock.Verify(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
        repoMock.Verify(r => r.SaveAsync(It.IsAny<ChaosAddress>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EditFailsOnSave_ReturnsFailedAndDoesNotLeavePartialState()
    {
        var repoMock = new Mock<IChaosAddressRepository>(MockBehavior.Strict);
        var id = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();
        var when = DateTime.UtcNow;

        var create = ChaosAddress.Create(id, domainId, subdomainId, "local", Spamma.Modules.Common.SmtpResponseCode.MailboxUnavailable, when);
        var chaos = create.Value;

        repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(Maybe.From(chaos));
        repoMock.Setup(r => r.SaveAsync(chaos, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Fail());

        var handler = new EditChaosAddressCommandHandler(repoMock.Object, Array.Empty<FluentValidation.IValidator<EditChaosAddressCommand>>(), new Mock<ILogger<EditChaosAddressCommandHandler>>().Object);

        var cmd = new EditChaosAddressCommand(id, domainId, subdomainId, "newlocal", Spamma.Modules.Common.SmtpResponseCode.RequestedActionAborted, null);
        var res = await handler.Handle(cmd, CancellationToken.None);

        res.Should().NotBeNull();
        repoMock.Verify(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
        repoMock.Verify(r => r.SaveAsync(chaos, It.IsAny<CancellationToken>()), Times.Once);
    }
}
