using FluentAssertions;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.DomainManagement.Application.Validators.Subdomain;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Subdomain;

namespace Spamma.Modules.DomainManagement.Tests.Application.Validators;

public class CheckMxRecordCommandValidatorTests
{
    [Fact]
    public void Validate_WithValidSubdomainId_ShouldNotHaveErrors()
    {
        // Arrange
        var validator = new CheckMxRecordCommandValidator();
        var command = new CheckMxRecordCommand(Guid.NewGuid());

        // Act
        var result = validator.Validate(command);

        // Verify
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithEmptySubdomainId_ShouldHaveRequiredError()
    {
        // Arrange
        var validator = new CheckMxRecordCommandValidator();
        var command = new CheckMxRecordCommand(Guid.Empty);

        // Act
        var result = validator.Validate(command);

        // Verify
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].PropertyName.Should().Be("SubdomainId");
        result.Errors[0].ErrorCode.Should().Be(CommonValidationCodes.Required);
        result.Errors[0].ErrorMessage.Should().Contain("required");
    }
}