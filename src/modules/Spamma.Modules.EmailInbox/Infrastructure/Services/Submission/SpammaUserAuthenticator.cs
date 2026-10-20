using Microsoft.Extensions.DependencyInjection;
using SmtpServer;
using SmtpServer.Authentication;

namespace Spamma.Modules.EmailInbox.Infrastructure.Services.Submission;

public sealed class SpammaUserAuthenticator : IUserAuthenticator
{
    public async Task<bool> AuthenticateAsync(
        ISessionContext context, string user, string password, CancellationToken cancellationToken)
    {
        if (!context.Pipe.IsSecure || !SmtpSubmissionMailboxFilter.IsSubmission(context))
        {
            return false;
        }

        using var scope = context.ServiceProvider.CreateScope();
        var policies = scope.ServiceProvider.GetRequiredService<SmtpSubmissionPolicyService>();
        return await policies.AuthenticateAsync(user, password, cancellationToken);
    }
}
