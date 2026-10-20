namespace Spamma.Modules.EmailInbox.Client.Application.Queries;

public sealed record EmailSearchParseResult(IReadOnlyList<EmailSearchToken> Tokens, string? Error)
{
    public bool IsValid => this.Error is null;
}
