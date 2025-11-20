using BluQube.Queries;
using Spamma.Modules.UserManagement.Application.Repositories;
using Spamma.Modules.UserManagement.Client.Application.Queries;

namespace Spamma.Modules.UserManagement.Application.QueryProcessors.Passkey;

internal class GetPasskeyByCredentialIdQueryProcessor(
    IPasskeyRepository passkeyRepository) : IQueryProcessor<GetPasskeyByCredentialIdQuery, GetPasskeyByCredentialIdQueryResult>
{
    public async Task<QueryResult<GetPasskeyByCredentialIdQueryResult>> Handle(GetPasskeyByCredentialIdQuery request, CancellationToken cancellationToken)
    {
        var passkeyMaybe = await passkeyRepository.GetByCredentialIdAsync(request.CredentialId, cancellationToken);

        if (passkeyMaybe.HasNoValue)
        {
            return QueryResult<GetPasskeyByCredentialIdQueryResult>.Failed();
        }

        var passkey = passkeyMaybe.Value;

        var result = new GetPasskeyByCredentialIdQueryResult(
            passkey.Id,
            passkey.UserId,
            passkey.PublicKey,
            passkey.SignCount,
            passkey.RegisteredAt,
            passkey.HasBeenUsed ? passkey.LastUsedAt : (DateTime?)null,
            passkey.IsRevoked,
            passkey.IsRevoked ? passkey.RevokedAt : (DateTime?)null,
            passkey.IsRevoked ? passkey.RevokedByUserId : (Guid?)null);

        return QueryResult<GetPasskeyByCredentialIdQueryResult>.Succeeded(result);
    }
}

