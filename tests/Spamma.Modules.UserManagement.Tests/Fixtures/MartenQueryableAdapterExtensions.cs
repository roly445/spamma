using Marten.Linq;

namespace Spamma.Modules.UserManagement.Tests.Fixtures;

/// <summary>
/// Helper extension to adapt IQueryable to IMartenQueryable for testing purposes.
/// This is a workaround for mocking since Marten's IMartenQueryable is designed specifically
/// for Marten operations and doesn't have a standard adapter.
/// </summary>
public static class MartenQueryableAdapterExtensions
{
    /// <summary>
    /// Wraps an IQueryable as an IMartenQueryable for testing.
    /// Note: This is a test helper that enables basic query testing without a real Marten session.
    /// </summary>
    public static IMartenQueryable<T> AsMartenQueryableForTesting<T>(this IQueryable<T> source)
    {
        // Cast the IQueryable to IMartenQueryable
        // In a real scenario, this would come from session.Query<T>()
        return source as IMartenQueryable<T> ?? 
            throw new InvalidOperationException("Cannot convert IQueryable to IMartenQueryable in test environment. Use real Marten session for integration tests.");
    }
}
