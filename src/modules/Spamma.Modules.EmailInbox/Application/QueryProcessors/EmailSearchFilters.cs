using System.Globalization;
using System.Text;
using Marten;
using Spamma.Modules.EmailInbox.Client.Contracts;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

namespace Spamma.Modules.EmailInbox.Application.QueryProcessors;

internal static class EmailSearchFilters
{
    public static IQueryable<EmailLookup> Apply(IQueryable<EmailLookup> query, string? searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return query;
        }

        foreach (var token in Tokenize(searchText))
        {
            var separator = token.IndexOf(':');
            var field = separator > 0 ? token[..separator].ToLowerInvariant() : string.Empty;
            var value = separator > 0 ? token[(separator + 1)..] : token;
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            var term = value.ToLowerInvariant();
            switch (field)
            {
                case "from":
                    query = query.Where(x => x.EmailAddresses.Any(a =>
                        a.EmailAddressType == EmailAddressType.From &&
                        (a.Address.ToLowerInvariant().Contains(term) || a.Name.ToLowerInvariant().Contains(term))));
                    break;
                case "to":
                    query = query.Where(x => x.EmailAddresses.Any(a =>
                        a.EmailAddressType == EmailAddressType.To &&
                        (a.Address.ToLowerInvariant().Contains(term) || a.Name.ToLowerInvariant().Contains(term))));
                    break;
                case "subject":
                    query = query.Where(x => x.Subject.ToLowerInvariant().Contains(term));
                    break;
                case "body":
                    query = value.Contains(' ')
                        ? query.Where(x => x.PhraseSearch(value))
                        : query.Where(x => x.PlainTextSearch(value));
                    break;
                case "before" when TryDate(value, out var before):
                    query = query.Where(x => x.SentAt < before);
                    break;
                case "after" when TryDate(value, out var after):
                    query = query.Where(x => x.SentAt >= after);
                    break;
                default:
                    // Preserve ordinary text search, including text containing a colon.
                    var plainTerm = token.ToLowerInvariant();
                    query = token.Contains(' ')
                        ? query.Where(x => x.Subject.ToLowerInvariant().Contains(plainTerm) ||
                            x.EmailAddresses.Any(a => a.Address.ToLowerInvariant().Contains(plainTerm) ||
                                                      a.Name.ToLowerInvariant().Contains(plainTerm)) ||
                            x.PhraseSearch(token))
                        : query.Where(x => x.Subject.ToLowerInvariant().Contains(plainTerm) ||
                            x.EmailAddresses.Any(a => a.Address.ToLowerInvariant().Contains(plainTerm) ||
                                                      a.Name.ToLowerInvariant().Contains(plainTerm)) ||
                            x.PlainTextSearch(token));
                    break;
            }
        }

        return query;
    }

    private static bool TryDate(string value, out DateTimeOffset date) =>
        DateTimeOffset.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out date);

    private static IEnumerable<string> Tokenize(string input)
    {
        var token = new StringBuilder();
        var inQuotes = false;
        foreach (var character in input)
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
                    yield return token.ToString();
                    token.Clear();
                }

                continue;
            }

            token.Append(character);
        }

        if (token.Length > 0)
        {
            yield return token.ToString();
        }
    }
}
