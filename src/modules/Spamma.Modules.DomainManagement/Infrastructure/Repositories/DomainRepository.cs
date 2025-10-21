using Marten;
using Spamma.Modules.Common.Infrastructure;
using Spamma.Modules.DomainManagement.Application.Repositories;

namespace Spamma.Modules.DomainManagement.Infrastructure.Repositories;

public class DomainRepository(IDocumentSession session) : GenericRepository<Domain.DomainAggregate.Domain>(session), IDomainRepository;