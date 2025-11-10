using FluentAssertions;
using Spamma.Modules.EmailInbox.Application.AuthorizationRequirements;

namespace Spamma.Modules.EmailInbox.Tests.Application.AuthorizationRequirements;

public class MustHaveAccessToAtLeastOneCampaignRequirementTests
{
    [Fact]
    public void Requirement_CreatesSuccessfully()
    {
        var requirement = new MustHaveAccessToAtLeastOneCampaignRequirement();

        requirement.Should().NotBeNull();
    }
}
