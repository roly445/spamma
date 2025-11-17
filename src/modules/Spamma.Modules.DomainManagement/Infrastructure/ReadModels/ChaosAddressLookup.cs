using Spamma.Modules.Common.Client;

namespace Spamma.Modules.DomainManagement.Infrastructure.ReadModels;

#pragma warning disable S1144 // Private setters are used by Marten's Patch API via reflection

public class ChaosAddressLookup
{
    public Guid Id { get; internal set; }

    public Guid DomainId { get; internal set; }

    public Guid SubdomainId { get; internal set; }

    public string LocalPart { get; internal set; } = string.Empty;

    public SmtpResponseCode ConfiguredSmtpCode { get; internal set; }

    public bool Enabled { get; internal set; }

    public bool ImmutableAfterFirstReceive { get; internal set; }

    public int TotalReceived { get; internal set; }

    public DateTimeOffset? LastReceivedAt { get; internal set; }

    public DateTime CreatedAt { get; internal set; }

    public Guid CreatedBy { get; internal set; }
}