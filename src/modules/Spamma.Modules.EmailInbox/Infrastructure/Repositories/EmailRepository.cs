using Marten;
using Spamma.Modules.Common.Infrastructure;
using Spamma.Modules.EmailInbox.Application.Repositories;

namespace Spamma.Modules.EmailInbox.Infrastructure.Repositories;

public class EmailRepository(IDocumentSession session) : GenericRepository<Domain.EmailAggregate.Email>(session), IEmailRepository;