using Marten;
using Spamma.Modules.Common.Infrastructure;
using Spamma.Modules.EmailInbox.Application.Repositories;
using Spamma.Modules.EmailInbox.Domain.EmailAggregate;

namespace Spamma.Modules.EmailInbox.Infrastructure.Repositories;

public class EmailRepository(IDocumentSession session) : GenericRepository<Domain.EmailAggregate.Email>(session), IEmailRepository;