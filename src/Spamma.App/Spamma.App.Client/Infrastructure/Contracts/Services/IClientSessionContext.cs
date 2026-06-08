namespace Spamma.App.Client.Infrastructure.Contracts.Services;

public interface IClientSessionContext
{
    string CurrentRoute { get; }

    Task InitializeAsync();

    Task<string> GetSessionIdAsync();

    Task TrackBreadcrumbAsync(string name, Dictionary<string, string?>? data = null);
}
