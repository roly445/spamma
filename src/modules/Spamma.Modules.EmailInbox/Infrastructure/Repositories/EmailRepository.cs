using Marten;
using Microsoft.Extensions.Hosting;
using MimeKit;
using Spamma.Modules.Common.Infrastructure;
using Spamma.Modules.EmailInbox.Application.Repositories;

namespace Spamma.Modules.EmailInbox.Infrastructure.Repositories;

public class EmailRepository(IDocumentSession session, IHostEnvironment hostEnvironment) : GenericRepository<Domain.EmailAggregate.Email>(session), IEmailRepository
{
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
}