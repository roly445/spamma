using Spamma.Modules.Common.Application.Contracts;

namespace Spamma.Modules.DomainManagement.Application.Repositories;

public interface IDomainRepository : IRepository<Domain.DomainAggregate.Domain>;