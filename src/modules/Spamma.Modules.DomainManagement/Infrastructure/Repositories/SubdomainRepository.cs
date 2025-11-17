using Marten;
using Spamma.Modules.Common.Infrastructure;
using Spamma.Modules.DomainManagement.Application.Repositories;
using Spamma.Modules.DomainManagement.Domain.SubdomainAggregate;

namespace Spamma.Modules.DomainManagement.Infrastructure.Repositories;

internal class SubdomainRepository(IDocumentSession session) : GenericRepository<Subdomain>(session), ISubdomainRepository;