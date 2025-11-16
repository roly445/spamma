using Marten;
using Spamma.Modules.Common.Infrastructure;
using Spamma.Modules.DomainManagement.Application.Repositories;
using Spamma.Modules.DomainManagement.Domain.ChaosAddressAggregate;

namespace Spamma.Modules.DomainManagement.Infrastructure.Repositories;

internal class ChaosAddressRepository(IDocumentSession session) : GenericRepository<ChaosAddress>(session), IChaosAddressRepository;