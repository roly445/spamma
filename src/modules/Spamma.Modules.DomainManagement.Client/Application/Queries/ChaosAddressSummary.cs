using Spamma.Modules.Common.Client;

namespace Spamma.Modules.DomainManagement.Client.Application.Queries;

public record ChaosAddressSummary(
    Guid ChaosAddressId,
    Guid DomainId,
    Guid SubdomainId,
    string LocalPart,
    SmtpResponseCode ConfiguredSmtpCode,
    bool Enabled,
    int TotalReceived,
    DateTimeOffset? LastReceivedAt,
    DateTime CreatedAt);