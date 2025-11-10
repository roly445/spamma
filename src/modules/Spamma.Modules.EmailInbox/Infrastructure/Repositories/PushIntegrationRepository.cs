using Marten;
using Spamma.Modules.Common.Infrastructure;
using Spamma.Modules.EmailInbox.Application.Repositories;

namespace Spamma.Modules.EmailInbox.Infrastructure.Repositories;

/// <summary>
/// Repository for push integrations.
/// </summary>
public class PushIntegrationRepository(IDocumentSession session)
    : GenericRepository<Domain.PushIntegrationAggregate.PushIntegration>(session), IPushIntegrationRepository;