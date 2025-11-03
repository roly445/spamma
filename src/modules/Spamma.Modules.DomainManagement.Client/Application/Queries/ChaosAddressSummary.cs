using Spamma.Modules.Common;

namespace Spamma.Modules.DomainManagement.Client.Application.Queries;

public record ChaosAddressSummary(
    Guid Id,
    Guid DomainId,
    Guid SubdomainId,
    string LocalPart,
    SmtpResponseCode ConfiguredSmtpCode,
    bool Enabled,
    int TotalReceived,
    DateTime? LastReceivedAt,
    DateTime CreatedAt,
    Guid CreatedBy);
