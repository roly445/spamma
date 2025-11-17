using MimeKit;
using Spamma.Modules.Common.Application.Contracts;
using Spamma.Modules.EmailInbox.Domain.EmailAggregate;

namespace Spamma.Modules.EmailInbox.Application.Repositories;

internal interface IEmailRepository : IRepository<Email>
{
    Task<MimeMessage?> GetMimeMessageAsync(Guid emailId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Email>> GetByCampaignIdAsync(Guid campaignId, CancellationToken cancellationToken);
}