using BluQube.Queries;

namespace Spamma.Modules.UserManagement.Application.Queries;

internal record GetPasskeyByCredentialIdQuery(byte[] CredentialId) : IQuery<GetPasskeyByCredentialIdQueryResult>;
