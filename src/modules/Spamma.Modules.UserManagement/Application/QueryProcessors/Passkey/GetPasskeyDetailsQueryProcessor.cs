using BluQube.Queries;
using Microsoft.Extensions.Logging;
using Spamma.Modules.UserManagement.Application.Repositories;
using Spamma.Modules.UserManagement.Client.Application.Queries;

namespace Spamma.Modules.UserManagement.Application.QueryProcessors.Passkey;

internal class GetPasskeyDetailsQueryProcessor(
    IPasskeyRepository passkeyRepository,
    ILogger<GetPasskeyDetailsQueryProcessor> logger) : IQueryProcessor<GetPasskeyDetailsQuery, PasskeyDetailsResult>
{
    public async Task<QueryResult<PasskeyDetailsResult>> Handle(GetPasskeyDetailsQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var passkeyMaybe = await passkeyRepository.GetByIdAsync(query.PasskeyId, cancellationToken);
            if (passkeyMaybe.HasNoValue)
            {
                return QueryResult<PasskeyDetailsResult>.Failed();
            }

            var passkey = passkeyMaybe.Value;

            var result = new PasskeyDetailsResult(
                passkey.Id,
                passkey.UserId,
                passkey.DisplayName,
                passkey.Algorithm,
                passkey.RegisteredAt,
                passkey.LastUsedAt,
                passkey.IsRevoked,
                passkey.RevokedAt,
                passkey.RevokedByUserId);

            return QueryResult<PasskeyDetailsResult>.Succeeded(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving passkey details for {PasskeyId}", query.PasskeyId);
            return QueryResult<PasskeyDetailsResult>.Failed();
        }
    }
}