using FluentAssertions;
using Spamma.Modules.EmailInbox.Client.Application.Queries;

namespace Spamma.Modules.EmailInbox.Tests.Application.QueryProcessors;

public class EmailSearchSyntaxTests
{
    [Theory]
    [InlineData("subject:", "Add a value after subject:.")]
    [InlineData("from:\"\"", "Add a value after from:.")]
    [InlineData("before:2026-02-30", "Use YYYY-MM-DD for before: (for example, before:2026-01-15).")]
    [InlineData("after:yesterday", "Use YYYY-MM-DD for after: (for example, after:2026-01-15).")]
    [InlineData("has:image", "Use has:attachment to find messages with attachments.")]
    [InlineData("subject:\"open phrase", "Close the quotation mark in your search.")]
    public void Parse_InvalidFilter_ReturnsActionableError(string search, string expectedError)
    {
        var parsed = EmailSearchSyntax.Parse(search);

        parsed.IsValid.Should().BeFalse();
        parsed.Error.Should().Be(expectedError);
        parsed.Tokens.Should().BeEmpty();
    }

    [Fact]
    public void Parse_ValidFiltersAndColonText_PreservesTokens()
    {
        var parsed = EmailSearchSyntax.Parse("subject:\"billing invoice\" has:attachment after:2026-01-15 ref:42");

        parsed.IsValid.Should().BeTrue();
        parsed.Tokens.Select(token => (token.Field, token.Value)).Should().Equal(
            ("subject", "billing invoice"),
            ("has", "attachment"),
            ("after", "2026-01-15"),
            (string.Empty, "ref:42"));
    }
}
