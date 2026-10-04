using System.Security.Claims;
using BluQube.Queries;
using FluentAssertions;
using Marten;
using Microsoft.AspNetCore.Http;
using Moq;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.EmailInbox.Application.Authorizers.Commands.Campaign;
using Spamma.Modules.EmailInbox.Application.Authorizers.Queries;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Campaign;
using Spamma.Modules.EmailInbox.Client.Application.Queries;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

namespace Spamma.Modules.EmailInbox.Tests.Application.Authorizers.Queries;

public class CampaignAuthorizationTests
{
    [Fact]
    public async Task List_ViewerAssignedToRequestedSubdomain_AllowsAccess()
    {
        var assignedSubdomainId = Guid.NewGuid();
        var authorizer = new GetCampaignsQueryAuthorizer(
            CreateAccessor(Lookups.ViewableSubdomainClaim, assignedSubdomainId), Mock.Of<IQueryRunner>());

        var result = await authorizer.Authorize(new GetCampaignsQuery(assignedSubdomainId), CancellationToken.None);

        result.IsAuthorized.Should().BeTrue();
    }

    [Fact]
    public async Task List_ViewerAssignedToDifferentSubdomain_DeniesAccess()
    {
        var authorizer = new GetCampaignsQueryAuthorizer(
            CreateAccessor(Lookups.ViewableSubdomainClaim, Guid.NewGuid()), Mock.Of<IQueryRunner>());

        var result = await authorizer.Authorize(new GetCampaignsQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsAuthorized.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_ViewerWithoutManagementAssignment_DeniesAccess()
    {
        var campaignId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();
        var session = new Mock<IDocumentSession>();
        session.Setup(x => x.LoadAsync<CampaignSummary>(campaignId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CampaignSummary
            {
                CampaignId = campaignId,
                DomainId = Guid.NewGuid(),
                SubdomainId = subdomainId,
            });
        var authorizer = new DeleteCampaignCommandAuthorizer(
            CreateAccessor(Lookups.ViewableSubdomainClaim, subdomainId), session.Object);

        var result = await authorizer.Authorize(new DeleteCampaignCommand(campaignId), CancellationToken.None);

        result.IsAuthorized.Should().BeFalse();
    }

    private static HttpContextAccessor CreateAccessor(string assignmentClaimType, Guid subdomainId)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Name, "Campaign user"),
            new Claim(ClaimTypes.Email, "campaign@example.test"),
            new Claim(assignmentClaimType, subdomainId.ToString()),
        };

        return new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth")),
            },
        };
    }
}
