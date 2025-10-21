namespace Spamma.App.Client.Infrastructure.Contracts.Services;

public interface IDomainValidationService
{
    Task BuildDomainListAsync(CancellationToken cancellationToken = default);

    bool IsDomainValid(string domain);

    bool IsSubdomainValid(string domain);
}