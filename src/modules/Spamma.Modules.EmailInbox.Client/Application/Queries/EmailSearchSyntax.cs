using System.Globalization;
using System.Text;

namespace Spamma.Modules.EmailInbox.Client.Application.Queries;

public static class EmailSearchSyntax
{
    public static EmailSearchParseResult Parse(string? searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return new EmailSearchParseResult([], null);
        }

        var tokens = new List<EmailSearchToken>();
        var token = new StringBuilder();
        var inQuotes = false;
        foreach (var character in searchText)
        {
            if (character == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (char.IsWhiteSpace(character) && !inQuotes)
            {
                if (token.Length > 0)
                {
                    tokens.Add(ParseToken(token.ToString()));
                    token.Clear();
                }

                continue;
            }

            token.Append(character);
        }

        if (inQuotes)
        {
            return new EmailSearchParseResult([], "Close the quotation mark in your search.");
        }

        if (token.Length > 0)
        {
            tokens.Add(ParseToken(token.ToString()));
        }

        foreach (var searchToken in tokens)
        {
            if (searchToken.Field.Length == 0)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(searchToken.Value))
            {
                return new EmailSearchParseResult([], $"Add a value after {searchToken.Field}:.");
            }

            if (searchToken.Field is "before" or "after" && !TryDate(searchToken.Value, out _))
            {
                return new EmailSearchParseResult([], $"Use YYYY-MM-DD for {searchToken.Field}: (for example, {searchToken.Field}:2026-01-15).");
            }

            if (searchToken.Field == "has" && !searchToken.Value.Equals("attachment", StringComparison.OrdinalIgnoreCase))
            {
                return new EmailSearchParseResult([], "Use has:attachment to find messages with attachments.");
            }
        }

        return new EmailSearchParseResult(tokens, null);
    }

    public static bool TryDate(string value, out DateTimeOffset date) =>
        DateTimeOffset.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out date);

    private static EmailSearchToken ParseToken(string raw)
    {
        var separator = raw.IndexOf(':');
        if (separator <= 0)
        {
            return new EmailSearchToken(string.Empty, raw, raw);
        }

        var field = raw[..separator].ToLowerInvariant();
        return field is "from" or "to" or "subject" or "body" or "before" or "after" or "has"
            ? new EmailSearchToken(field, raw[(separator + 1)..], raw)
            : new EmailSearchToken(string.Empty, raw, raw);
    }
}
