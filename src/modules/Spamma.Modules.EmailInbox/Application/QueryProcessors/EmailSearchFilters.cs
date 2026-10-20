using Marten;
using Spamma.Modules.EmailInbox.Client.Application.Queries;
using Spamma.Modules.EmailInbox.Client.Contracts;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

namespace Spamma.Modules.EmailInbox.Application.QueryProcessors;

internal static class EmailSearchFilters
{
    public static IQueryable<EmailLookup> Apply(IQueryable<EmailLookup> query, string? searchText)
    {
        var parsed = EmailSearchSyntax.Parse(searchText);
        if (!parsed.IsValid)
        {
            throw new ArgumentException(parsed.Error, nameof(searchText));
        }

        return Apply(query, parsed);
    }

    public static IQueryable<EmailLookup> Apply(IQueryable<EmailLookup> query, EmailSearchParseResult parsed)
    {
        foreach (var token in parsed.Tokens)
        {
            var field = token.Field;
            var value = token.Value;
            var term = value.ToLowerInvariant();
            switch (field)
            {
                case "has" when value.Equals("attachment", StringComparison.OrdinalIgnoreCase):
                    query = query.Where(x => x.HasAttachment == true);
                    break;
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
                case "before" when EmailSearchSyntax.TryDate(value, out var before):
                    query = query.Where(x => x.SentAt < before);
                    break;
                case "after" when EmailSearchSyntax.TryDate(value, out var after):
                    query = query.Where(x => x.SentAt >= after);
                    break;
                default:
                    // Preserve ordinary text search, including text containing a colon.
                    var plainTerm = token.Raw.ToLowerInvariant();
                    query = token.Raw.Contains(' ')
                        ? query.Where(x => x.Subject.ToLowerInvariant().Contains(plainTerm) ||
                            x.EmailAddresses.Any(a => a.Address.ToLowerInvariant().Contains(plainTerm) ||
                                                      a.Name.ToLowerInvariant().Contains(plainTerm)) ||
                            x.PhraseSearch(token.Raw))
                        : query.Where(x => x.Subject.ToLowerInvariant().Contains(plainTerm) ||
                            x.EmailAddresses.Any(a => a.Address.ToLowerInvariant().Contains(plainTerm) ||
                                                      a.Name.ToLowerInvariant().Contains(plainTerm)) ||
                            x.PlainTextSearch(token.Raw));
                    break;
            }
        }

        return query;
    }
}
