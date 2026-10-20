using FluentAssertions;
using Spamma.Modules.EmailInbox.Application.QueryProcessors;
using Spamma.Modules.EmailInbox.Client.Contracts;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

namespace Spamma.Modules.EmailInbox.Tests.Application.QueryProcessors;

public class EmailSearchFiltersTests
{
    private static readonly Guid VisibleSubdomain = Guid.NewGuid();
    private static readonly Guid OtherSubdomain = Guid.NewGuid();

    [Theory]
    [InlineData("from:alice", "Billing invoice")]
    [InlineData("to:bob", "Billing invoice")]
    [InlineData("subject:invoice", "Billing invoice")]
    [InlineData("subject:\"billing invoice\"", "Billing invoice")]
    [InlineData("after:2026-01-01 before:2026-02-01", "Billing invoice")]
    [InlineData("from:alice subject:invoice", "Billing invoice")]
    public void Apply_FiltersByFieldAndDate(string search, string expectedSubject)
    {
        var messages = CreateMessages().AsQueryable();

        var results = EmailSearchFilters.Apply(messages, search).ToList();

        results.Should().ContainSingle().Which.Subject.Should().Be(expectedSubject);
    }

    [Fact]
    public void Apply_PreservesAccessPredicate()
    {
        var scopedMessages = CreateMessages().AsQueryable().Where(x => x.SubdomainId == VisibleSubdomain);

        var results = EmailSearchFilters.Apply(scopedMessages, "invoice").ToList();

        results.Should().ContainSingle().Which.SubdomainId.Should().Be(VisibleSubdomain);
    }

    private static List<EmailLookup> CreateMessages() =>
    [
        new()
        {
            Id = Guid.NewGuid(),
            SubdomainId = VisibleSubdomain,
            Subject = "Billing invoice",
            SentAt = new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero),
            EmailAddresses =
            [
                new("alice@example.com", "Alice", EmailAddressType.From),
                new("bob@example.com", "Bob", EmailAddressType.To),
            ],
        },
        new()
        {
            Id = Guid.NewGuid(),
            SubdomainId = OtherSubdomain,
            Subject = "Other notice",
            SentAt = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero),
            EmailAddresses =
            [
                new("carol@example.com", "Carol", EmailAddressType.From),
                new("dave@example.com", "Dave", EmailAddressType.To),
            ],
        },
    ];
}
