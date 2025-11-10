using Spamma.Modules.Common.Application.Contracts;
using Spamma.Modules.EmailInbox.Domain.PushIntegrationAggregate;

namespace Spamma.Modules.EmailInbox.Application.Repositories;

/// <summary>
/// Repository interface for push integrations.
/// </summary>
public interface IPushIntegrationRepository : IRepository<PushIntegration>;