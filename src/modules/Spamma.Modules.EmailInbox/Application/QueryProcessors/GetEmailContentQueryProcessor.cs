using BluQube.Queries;
using MimeKit;
using Spamma.Modules.EmailInbox.Application.Repositories;
using Spamma.Modules.EmailInbox.Client.Application.Queries;

namespace Spamma.Modules.EmailInbox.Application.QueryProcessors;

internal class GetEmailContentQueryProcessor(
    IEmailRepository emailRepository)
    : IQueryProcessor<GetEmailContentQuery, GetEmailContentQueryResult>
{
    public async Task<QueryResult<GetEmailContentQueryResult>> Handle(
        GetEmailContentQuery query,
        CancellationToken cancellationToken)
    {
        var email = await emailRepository.GetByIdAsync(query.EmailId, cancellationToken);
        if (email.HasNoValue)
        {
            return QueryResult<GetEmailContentQueryResult>.Succeeded(
                new GetEmailContentQueryResult(string.Empty, "text/plain"));
        }

        // Load the MIME message from storage
        var mimeMessage = await emailRepository.GetMimeMessageAsync(query.EmailId, cancellationToken);
        if (mimeMessage == null)
        {
            return QueryResult<GetEmailContentQueryResult>.Succeeded(
                new GetEmailContentQueryResult(string.Empty, "text/plain"));
        }

        // Generate EML content
        using var memoryStream = new MemoryStream();
        await mimeMessage.WriteToAsync(memoryStream, cancellationToken);
        var emlContent = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());

        return QueryResult<GetEmailContentQueryResult>.Succeeded(
            new GetEmailContentQueryResult(emlContent, "message/rfc822"));
    }
}