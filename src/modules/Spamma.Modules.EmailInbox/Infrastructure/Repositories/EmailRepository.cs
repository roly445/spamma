using Marten;
using Microsoft.Extensions.Hosting;
using MimeKit;
using Spamma.Modules.Common.Infrastructure;
using Spamma.Modules.EmailInbox.Application.Repositories;

namespace Spamma.Modules.EmailInbox.Infrastructure.Repositories;

internal class EmailRepository(IDocumentSession session, IHostEnvironment hostEnvironment) : GenericRepository<Domain.EmailAggregate.Email>(session), IEmailRepository
{
    private readonly IDocumentSession _session = session;

    public async Task<MimeMessage?> GetMimeMessageAsync(Guid emailId, CancellationToken cancellationToken)
    {
        var filePath = Path.Combine(hostEnvironment.ContentRootPath, "messages", $"{emailId}.eml");
        if (!File.Exists(filePath))
        {
            return null;
        }

        await using var stream = File.OpenRead(filePath);
        return await MimeMessage.LoadAsync(stream, cancellationToken);
    }

    public async Task<IReadOnlyList<Domain.EmailAggregate.Email>> GetByCampaignIdAsync(Guid campaignId, CancellationToken cancellationToken)
    {
        var emailLookups = await this._session.Query<Infrastructure.ReadModels.EmailLookup>()
            .Where(e => e.CampaignId == campaignId && e.DeletedAt == null)
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);

        var emails = new List<Domain.EmailAggregate.Email>();
        foreach (var emailId in emailLookups)
        {
            var email = await this.GetByIdAsync(emailId, cancellationToken);
            if (email.HasValue)
            {
                emails.Add(email.Value);
            }
        }

        return emails;
    }
}