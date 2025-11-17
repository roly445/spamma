using Spamma.Modules.DomainManagement.Client.Contracts;

namespace Spamma.Modules.DomainManagement.Domain.ChaosAddressAggregate;

public class ChaosAddressSuspensionAudit
{
    private ChaosAddressSuspensionAudit(
        DateTime happenedAt, ChaosAddressSuspensionAuditType type)
    {
        this.HappenedAt = happenedAt;
        this.Type = type;
    }

    internal DateTime HappenedAt { get; private set; }

    internal ChaosAddressSuspensionAuditType Type { get; private set; }

    internal static ChaosAddressSuspensionAudit CreateSuspension(
        DateTime happenedAt)
    {
        return new ChaosAddressSuspensionAudit(happenedAt, ChaosAddressSuspensionAuditType.Suspend);
    }

    internal static ChaosAddressSuspensionAudit CreateUnsuspension(
        DateTime happenedAt)
    {
        return new ChaosAddressSuspensionAudit(happenedAt, ChaosAddressSuspensionAuditType.Unsuspend);
    }
}