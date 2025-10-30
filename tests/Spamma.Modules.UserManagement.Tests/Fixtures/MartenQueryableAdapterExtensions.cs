using Marten.Linq;

namespace Spamma.Modules.UserManagement.Tests.Fixtures;

public static class MartenQueryableAdapterExtensions
{
    public static IMartenQueryable<T> AsMartenQueryableForTesting<T>(this IQueryable<T> source)
    {
        return source as IMartenQueryable<T> ??
            throw new InvalidOperationException("Cannot convert IQueryable to IMartenQueryable in test environment. Use real Marten session for integration tests.");
    }
}