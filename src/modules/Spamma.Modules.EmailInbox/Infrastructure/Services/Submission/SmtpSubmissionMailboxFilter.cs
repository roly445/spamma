using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SmtpServer;
using SmtpServer.Mail;
using SmtpServer.Storage;
using Spamma.Modules.Common.Caching;
using Spamma.Modules.EmailInbox.Infrastructure.Settings;

namespace Spamma.Modules.EmailInbox.Infrastructure.Services.Submission;

public sealed class SmtpSubmissionMailboxFilter : IMailboxFilter
{
    private const string RejectedRecipientsKey = "Spamma.SmtpSubmission.RejectedRecipients";

    public Task<bool> CanAcceptFromAsync(
        ISessionContext context, IMailbox from, int size, CancellationToken cancellationToken)
    {
        // MAIL FROM starts a new transaction, including after RSET.
        context.Properties?.Remove(RejectedRecipientsKey);
        return Task.FromResult(true);
    }

    public async Task<bool> CanDeliverToAsync(
        ISessionContext context, IMailbox to, IMailbox from, CancellationToken cancellationToken)
    {
        if (!IsSubmission(context))
        {
            return true; // MX delivery on port 25 keeps its existing anonymous behavior.
        }

        if (!context.Pipe.IsSecure)
        {
            return Reject(context);
        }

        using var scope = context.ServiceProvider.CreateScope();
        var subdomains = scope.ServiceProvider.GetRequiredService<ISubdomainCache>();
        var lookup = await subdomains.GetSubdomainAsync(to.Host, cancellationToken: cancellationToken);
        if (!lookup.HasValue)
        {
            return Reject(context);
        }

        var policies = scope.ServiceProvider.GetRequiredService<SmtpSubmissionPolicyService>();
        var effective = await policies.GetEffectiveAsync(
            lookup.Value.DomainId, lookup.Value.SubdomainId, cancellationToken);
        return SmtpSubmissionPolicyService.Allows(effective, context.Authentication.User) || Reject(context);
    }

    internal static bool HadRejectedRecipient(ISessionContext context) =>
        context.Properties is not null &&
        context.Properties.ContainsKey(RejectedRecipientsKey);

    internal static bool IsSubmission(ISessionContext context)
    {
        var port = context.EndpointDefinition?.Endpoint.Port;
        if (port is null)
        {
            return false;
        }

        var settings = context.ServiceProvider.GetService<IOptions<EmailInboxSettings>>()?.Value ?? new EmailInboxSettings();
        return port == settings.ImplicitTlsPort || port == settings.StartTlsPort;
    }

    private static bool Reject(ISessionContext context)
    {
        if (context.Properties is not null)
        {
            // SMTP's rejected RCPT is not added to transaction.To. Remember the
            // rejection so DATA rejects the whole transaction.
            context.Properties[RejectedRecipientsKey] = true;
        }

        return false;
    }
}
