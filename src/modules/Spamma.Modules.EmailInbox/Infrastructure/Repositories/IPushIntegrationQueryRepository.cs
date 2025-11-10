using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

namespace Spamma.Modules.EmailInbox.Infrastructure.Repositories;

/// <summary>
/// Repository for querying push integrations.
/// </summary>
public interface IPushIntegrationQueryRepository
{
    /// <summary>
    /// Gets push integrations by user ID.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>The push integrations.</returns>
    Task<IEnumerable<PushIntegrationLookup>> GetByUserIdAsync(Guid userId);
}