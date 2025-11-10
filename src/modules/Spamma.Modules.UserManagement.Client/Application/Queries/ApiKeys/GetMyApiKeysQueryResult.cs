using BluQube.Queries;
using Spamma.Modules.UserManagement.Client.Application.DTOs;

namespace Spamma.Modules.UserManagement.Client.Application.Queries.ApiKeys;

public record GetMyApiKeysQueryResult(IEnumerable<ApiKeySummary> ApiKeys) : IQueryResult;