using BluQube.Commands;
using Spamma.Modules.UserManagement.Client.Application.DTOs;

namespace Spamma.Modules.UserManagement.Client.Application.Commands.ApiKeys;

public record CreateApiKeyCommandResult(
    ApiKeySummary ApiKey,
    string KeyValue) : ICommandResult;