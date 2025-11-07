using BluQube.Queries;
using Spamma.Modules.Common.Client;

namespace Spamma.Modules.DomainManagement.Client.Application.Queries;

public record GetChaosAddressBySubdomainAndLocalPartQueryResult(
    Guid ChaosAddressId, Guid SubdomainId, string LocalPart, SmtpResponseCode ConfiguredSmtpCode, bool Enabled) : IQueryResult;
