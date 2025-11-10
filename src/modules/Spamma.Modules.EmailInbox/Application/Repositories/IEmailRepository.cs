using MimeKit;
using Spamma.Modules.Common.Application.Contracts;
using Spamma.Modules.EmailInbox.Domain.EmailAggregate;

namespace Spamma.Modules.EmailInbox.Application.Repositories;

public interface IEmailRepository : IRepository<Email>
{
    /// <summary>
    /// Gets the MIME message for an email.
    /// </summary>
    /// <param name="emailId">The email ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The MIME message.</returns>
    Task<MimeMessage?> GetMimeMessageAsync(Guid emailId, CancellationToken cancellationToken);
}