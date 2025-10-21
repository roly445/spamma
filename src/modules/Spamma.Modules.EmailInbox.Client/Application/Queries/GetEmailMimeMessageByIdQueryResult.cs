using BluQube.Queries;

namespace Spamma.Modules.EmailInbox.Client.Application.Queries;

public record GetEmailMimeMessageByIdQueryResult(string FileContent) : IQueryResult;