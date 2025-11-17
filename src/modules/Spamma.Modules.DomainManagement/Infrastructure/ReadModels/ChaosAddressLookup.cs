using Spamma.Modules.Common.Client;

namespace Spamma.Modules.DomainManagement.Infrastructure.ReadModels;

public class ChaosAddressLookup
{
    public Guid Id { get; init; }

    public Guid DomainId { get; init; }

    public Guid SubdomainId { get; init; }

    public string LocalPart { get; init; } = string.Empty;

    public SmtpResponseCode ConfiguredSmtpCode { get; init; }

    public bool Enabled { get; init; }

    public int TotalReceived { get; init; }

    public DateTimeOffset? LastReceivedAt { get; init; }

    public DateTime CreatedAt { get; init; }

    public Guid CreatedBy { get; init; }
}