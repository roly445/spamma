using Spamma.Modules.Common.Application.Contracts;

namespace Spamma.Modules.DomainManagement.Application.Repositories;

internal interface IDomainRepository : IRepository<Domain.DomainAggregate.Domain>;