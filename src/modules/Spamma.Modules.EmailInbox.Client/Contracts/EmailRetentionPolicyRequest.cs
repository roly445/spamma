namespace Spamma.Modules.EmailInbox.Client.Contracts;

public sealed record EmailRetentionPolicyRequest(EmailRetentionMode Mode, int? MaxAgeHours, int? MaxMessages);
