namespace Spamma.Modules.UserManagement.Infrastructure.Services.ApiKeys;

public interface IApiKeyValidationService
{
    Task<bool> ValidateApiKeyAsync(string apiKey, CancellationToken cancellationToken = default);
}