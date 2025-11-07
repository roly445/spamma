using FluentAssertions;
using Spamma.Modules.EmailInbox.Application.AuthorizationRequirements;

namespace Spamma.Modules.EmailInbox.Tests.Application.AuthorizationRequirements;

public class MustHaveAccessToSubdomainViaEmailRequirementTests
{
    [Fact]
    public void Requirement_WithValidEmailId_CreatesSuccessfully()
    {
        var emailId = Guid.NewGuid();

        var requirement = new MustHaveAccessToSubdomainViaEmailRequirement
        {
            EmailId = emailId,
        };

        requirement.Should().NotBeNull();
        requirement.EmailId.Should().Be(emailId);
    }

    [Fact]
    public void Requirement_WithEmptyEmailId_IsAllowed()
    {
        var requirement = new MustHaveAccessToSubdomainViaEmailRequirement
        {
            EmailId = Guid.Empty,
        };

        requirement.Should().NotBeNull();
    }
}
