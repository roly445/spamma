namespace Spamma.Modules.EmailInbox.Client.Contracts;

public sealed record EmailRetentionPolicyView(
    EmailRetentionMode Mode,
    int? MaxAgeHours,
    int? MaxMessages,
    int? EffectiveMaxAgeHours,
    int? EffectiveMaxMessages,
    string ScopeName,
    string? ParentDomainName);
