using Spamma.Modules.Common.Client;

namespace Spamma.Modules.DomainManagement.Infrastructure.ReadModels;

public class ChaosAddressLookup
{
    public Guid Id { get; set; }

    public Guid DomainId { get; set; }

    public Guid SubdomainId { get; set; }

    public string LocalPart { get; set; } = string.Empty;

    public SmtpResponseCode ConfiguredSmtpCode { get; set; }

    public bool Enabled { get; set; }

    public bool ImmutableAfterFirstReceive { get; set; }

    public int TotalReceived { get; set; }

    public DateTimeOffset? LastReceivedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid CreatedBy { get; set; }
}