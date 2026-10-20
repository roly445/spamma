using System.Security.Cryptography;
using System.Text;
using Marten;
using Spamma.Modules.EmailInbox.Client.Contracts;

namespace Spamma.Modules.EmailInbox.Infrastructure.Services.Submission;

public sealed class SmtpSubmissionPolicyService(IDocumentSession session)
{
    public static bool Allows(SmtpSubmissionPolicy? effective, string? authenticatedUser) =>
        effective is not { Mode: SmtpSubmissionMode.RequireAuthentication } ||
        string.Equals(effective.Username, authenticatedUser, StringComparison.Ordinal);

    public async Task<SmtpSubmissionPolicy?> GetDomainAsync(Guid domainId, CancellationToken cancellationToken) =>
        await session.LoadAsync<SmtpSubmissionPolicy>(SmtpSubmissionPolicy.DomainKey(domainId), cancellationToken);

    public async Task<SmtpSubmissionPolicy?> GetSubdomainAsync(Guid subdomainId, CancellationToken cancellationToken) =>
        await session.LoadAsync<SmtpSubmissionPolicy>(SmtpSubmissionPolicy.SubdomainKey(subdomainId), cancellationToken);

    public async Task<SmtpSubmissionPolicy?> GetEffectiveAsync(Guid domainId, Guid subdomainId, CancellationToken cancellationToken)
    {
        var subdomain = await this.GetSubdomainAsync(subdomainId, cancellationToken);
        if (subdomain is { Mode: not SmtpSubmissionMode.Inherit })
        {
            return subdomain;
        }

        return await this.GetDomainAsync(domainId, cancellationToken);
    }

    public async Task<(SmtpSubmissionPolicy Policy, string? Secret)> SetAsync(
        string id, SmtpSubmissionMode mode, CancellationToken cancellationToken)
    {
        string? secret = null;
        var credentialId = Guid.Empty;
        string? hash = null;
        if (mode == SmtpSubmissionMode.RequireAuthentication)
        {
            secret = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            credentialId = Guid.NewGuid();
            hash = Hash(secret);
        }

        var policy = new SmtpSubmissionPolicy(id, mode, credentialId, hash);
        session.Store(policy);
        await session.SaveChangesAsync(cancellationToken);
        return (policy, secret);
    }

    public async Task<bool> AuthenticateAsync(string username, string password, CancellationToken cancellationToken)
    {
        var lastColon = username.LastIndexOf(':');
        if (lastColon < 0 || !Guid.TryParseExact(username[(lastColon + 1)..], "N", out var credentialId))
        {
            return false;
        }

        var id = username[..lastColon];
        if (!IsValidKey(id))
        {
            return false;
        }

        var policy = await session.LoadAsync<SmtpSubmissionPolicy>(id, cancellationToken);
        if (policy is not { Mode: SmtpSubmissionMode.RequireAuthentication, SecretHash: not null } ||
            policy.CredentialId != credentialId || string.IsNullOrEmpty(password))
        {
            return false;
        }

        var expected = Convert.FromHexString(policy.SecretHash);
        var actual = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }

    private static string Hash(string secret) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));

    private static bool IsValidKey(string id)
    {
        var separator = id.IndexOf(':');
        return separator > 0 &&
               (id[..separator] == "domain" || id[..separator] == "subdomain") &&
               Guid.TryParseExact(id[(separator + 1)..], "N", out _);
    }
}
