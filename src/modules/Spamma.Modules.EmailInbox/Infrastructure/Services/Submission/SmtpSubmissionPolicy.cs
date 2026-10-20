using Spamma.Modules.EmailInbox.Client.Contracts;

namespace Spamma.Modules.EmailInbox.Infrastructure.Services.Submission;

public sealed record SmtpSubmissionPolicy(
    string Id,
    SmtpSubmissionMode Mode,
    Guid CredentialId,
    string? SecretHash)
{
    public string? Username => this.Mode == SmtpSubmissionMode.RequireAuthentication
        ? $"{this.Id}:{this.CredentialId:N}"
        : null;

    public static string DomainKey(Guid domainId) => $"domain:{domainId:N}";

    public static string SubdomainKey(Guid subdomainId) => $"subdomain:{subdomainId:N}";
}
