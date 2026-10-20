namespace Spamma.Modules.EmailInbox.Client.Contracts;

public sealed record SmtpSubmissionPolicyView(
    SmtpSubmissionMode Mode,
    SmtpSubmissionMode EffectiveMode,
    string? EffectiveSource,
    string? Username,
    string? Secret);
