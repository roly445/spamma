using BluQube.Commands;

namespace Spamma.Modules.UserManagement.Client.Application.Commands.ApiKeys;

public record CreateApiKeyCommandResult(
    ApiKeySummary ApiKey,
    string KeyValue) : ICommandResult;