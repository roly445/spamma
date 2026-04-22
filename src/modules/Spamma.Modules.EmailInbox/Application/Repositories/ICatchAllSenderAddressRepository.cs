using Spamma.Modules.Common.Application.Contracts;
using Spamma.Modules.EmailInbox.Domain.CatchAllSenderAddressAggregate;

namespace Spamma.Modules.EmailInbox.Application.Repositories;

internal interface ICatchAllSenderAddressRepository : IRepository<CatchAllSenderAddress>;
