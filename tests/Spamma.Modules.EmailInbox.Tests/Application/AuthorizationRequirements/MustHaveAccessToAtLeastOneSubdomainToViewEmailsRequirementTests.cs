using FluentAssertions;
using Spamma.Modules.EmailInbox.Application.AuthorizationRequirements;

namespace Spamma.Modules.EmailInbox.Tests.Application.AuthorizationRequirements;

public class MustHaveAccessToAtLeastOneSubdomainToViewEmailsRequirementTests
{
    [Fact]
    public void Requirement_CreatesSuccessfully()
    {
        var requirement = new MustHaveAccessToAtLeastOneSubdomainToViewEmailsRequirement();

        requirement.Should().NotBeNull();
    }
}
