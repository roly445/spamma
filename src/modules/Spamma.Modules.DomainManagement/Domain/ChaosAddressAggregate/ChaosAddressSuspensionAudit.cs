using Spamma.Modules.DomainManagement.Client.Contracts;

namespace Spamma.Modules.DomainManagement.Domain.ChaosAddressAggregate;

public class ChaosAddressSuspensionAudit
{
    private ChaosAddressSuspensionAudit(
        DateTime whenHappened, ChaosAddressSuspensionAuditType type)
    {
        this.WhenHappened = whenHappened;
        this.Type = type;
    }

    public DateTime WhenHappened { get; private set; }

    public ChaosAddressSuspensionAuditType Type { get; private set; }

    public static ChaosAddressSuspensionAudit CreateSuspension(
        DateTime whenHappened)
    {
        return new ChaosAddressSuspensionAudit(whenHappened, ChaosAddressSuspensionAuditType.Suspend);
    }

    public static ChaosAddressSuspensionAudit CreateUnsuspension(
        DateTime whenHappened)
    {
        return new ChaosAddressSuspensionAudit(whenHappened, ChaosAddressSuspensionAuditType.Unsuspend);
    }
}
