using Marten;
using Spamma.Modules.Common.Infrastructure;
using Spamma.Modules.EmailInbox.Application.Repositories;
using Spamma.Modules.EmailInbox.Domain.CatchAllSenderAddressAggregate;

namespace Spamma.Modules.EmailInbox.Infrastructure.Repositories;

internal class CatchAllSenderAddressRepository(IDocumentSession session)
    : GenericRepository<CatchAllSenderAddress>(session), ICatchAllSenderAddressRepository;
