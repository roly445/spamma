using System.IO.Compression;
using BluQube.Queries;
using Spamma.Modules.EmailInbox.Client.Application.Queries;
using Spamma.Modules.EmailInbox.Infrastructure.Services;

namespace Spamma.Modules.EmailInbox.Application.QueryProcessors;

public class GetEmailMimeMessageByIdQueryProcessor(IMessageStoreProvider messageStoreProvider) : IQueryProcessor<GetEmailMimeMessageByIdQuery, GetEmailMimeMessageByIdQueryResult>
{
    public async Task<QueryResult<GetEmailMimeMessageByIdQueryResult>> Handle(GetEmailMimeMessageByIdQuery request, CancellationToken cancellationToken)
    {
        var message = await messageStoreProvider.LoadMessageContentAsync(request.EmailId, cancellationToken);
        if (message.HasNoValue)
        {
            return QueryResult<GetEmailMimeMessageByIdQueryResult>.Failed();
        }

        using var ms = new MemoryStream();
        await message.Value.WriteToAsync(ms, cancellationToken);
        ms.Seek(0, SeekOrigin.Begin);
        using var outputStream = new MemoryStream();
        await using (var gzip = new GZipStream(outputStream, CompressionLevel.Optimal, leaveOpen: true))
        {
            await ms.CopyToAsync(gzip, cancellationToken);
        }

        outputStream.Seek(0, SeekOrigin.Begin);
        return QueryResult<GetEmailMimeMessageByIdQueryResult>.Succeeded(new GetEmailMimeMessageByIdQueryResult(Convert.ToBase64String(outputStream.ToArray())));
    }
}