using Marten;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

namespace Spamma.Modules.EmailInbox.Infrastructure.Repositories;

/// <summary>
/// Repository for querying push integrations.
/// </summary>
public class PushIntegrationQueryRepository(IDocumentSession documentSession) : IPushIntegrationQueryRepository
{
    /// <summary>
    /// Gets push integrations by user ID.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>The push integrations.</returns>
    public async Task<IEnumerable<PushIntegrationLookup>> GetByUserIdAsync(Guid userId)
    {
        return await documentSession.Query<PushIntegrationLookup>()
            .Where(x => x.UserId == userId && x.IsActive)
            .ToListAsync();
    }
}