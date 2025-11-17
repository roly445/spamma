using FluentAssertions;
using Spamma.Modules.EmailInbox.Application.Validators.Email;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Email;
using Spamma.Modules.EmailInbox.Client.Contracts;

namespace Spamma.Modules.EmailInbox.Tests.Application.Validators.Email;

public class ReceivedEmailCommandValidatorTests
{
    private readonly ReceivedEmailCommandValidator _validator;

    public ReceivedEmailCommandValidatorTests()
    {
        this._validator = new ReceivedEmailCommandValidator();
    }

    [Fact]
    public async Task Validate_WithValidCommand_Succeeds()
    {
        var command = new ReceivedEmailCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Subject",
            DateTime.UtcNow,
            new[] { new EmailAddress("test@example.com", "Test User", EmailAddressType.To) });

        var result = await this._validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithEmptyEmailAddresses_Fails()
    {
        var command = new ReceivedEmailCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Subject",
            DateTime.UtcNow,
            Array.Empty<EmailAddress>());

        var result = await this._validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EmailAddresses");
    }
}