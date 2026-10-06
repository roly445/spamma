using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common.Client;
using Spamma.Modules.EmailInbox.Application.Authorizers.Commands.CatchAllSender;
using Spamma.Modules.EmailInbox.Application.Authorizers.Commands.Email;
using Spamma.Modules.EmailInbox.Application.Authorizers.Queries;
using Spamma.Modules.EmailInbox.Client.Application.Commands.CatchAllSender;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Email;
using Spamma.Modules.EmailInbox.Client.Application.Queries;

namespace Spamma.Modules.EmailInbox.Tests.Application.Authorizers;

public class CatchAllAdministrationAuthorizationTests
{
    [Theory]
    [InlineData(SystemRole.DomainManagement, true)]
    [InlineData((SystemRole)0, false)]
    public async Task CatchAllAdministration_RequiresDomainManagement(SystemRole role, bool expected)
    {
        var accessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.Role, role.ToString()),
                ], "TestAuth")),
            },
        };
        var addressId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var results = new[]
        {
            await new UpdateCatchAllModeCommandAuthorizer(accessor).Authorize(new UpdateCatchAllModeCommand(true), CancellationToken.None),
            await new AddCatchAllSenderAddressCommandAuthorizer(accessor).Authorize(new AddCatchAllSenderAddressCommand("sender@example.test"), CancellationToken.None),
            await new AssignUserToCatchAllSenderCommandAuthorizer(accessor).Authorize(new AssignUserToCatchAllSenderCommand(addressId, userId), CancellationToken.None),
            await new UnassignUserFromCatchAllSenderCommandAuthorizer(accessor).Authorize(new UnassignUserFromCatchAllSenderCommand(addressId, userId), CancellationToken.None),
            await new RemoveCatchAllSenderAddressCommandAuthorizer(accessor).Authorize(new RemoveCatchAllSenderAddressCommand(addressId), CancellationToken.None),
            await new SearchCatchAllSenderAddressesQueryAuthorizer(accessor).Authorize(new SearchCatchAllSenderAddressesQuery(), CancellationToken.None),
            await new GetCatchAllSenderAddressDetailQueryAuthorizer(accessor).Authorize(new GetCatchAllSenderAddressDetailQuery(addressId), CancellationToken.None),
        };

        results.Should().OnlyContain(result => result.IsAuthorized == expected);
    }
}
