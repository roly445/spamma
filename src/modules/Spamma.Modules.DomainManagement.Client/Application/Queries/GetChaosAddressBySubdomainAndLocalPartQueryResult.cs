using BluQube.Queries;

namespace Spamma.Modules.DomainManagement.Client.Application.Queries;

public record GetChaosAddressBySubdomainAndLocalPartQueryResult(
    Guid Id, Guid SubdomainId, string LocalPart, Common.SmtpResponseCode ConfiguredSmtpCode, bool Enabled) : IQueryResult;
