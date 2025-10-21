using Spamma.Modules.DomainManagement.Client.Contracts;

namespace Spamma.Modules.DomainManagement.Domain.DomainAggregate;

public class DomainSuspensionAudit
{
    private readonly DomainSuspensionReason? _reason;

    private readonly string? _notes;

    private DomainSuspensionAudit(
        DateTime whenHappened, DomainSuspensionAuditType type,
        DomainSuspensionReason? reason = null, string? notes = null)
    {
        this.WhenHappened = whenHappened;
        this.Type = type;
        this._reason = reason;
        this._notes = notes;
    }

    public DateTime WhenHappened { get; private set; }

    public string? Notes =>
        this.Type == DomainSuspensionAuditType.Unsuspend ?
            throw new InvalidOperationException("Notes is not applicable for unsuspension.") :
            this._notes;

    public DomainSuspensionReason Reason =>
        this.Type == DomainSuspensionAuditType.Unsuspend ?
            throw new InvalidOperationException("Notes is not applicable for unsuspension.") :
            (DomainSuspensionReason)this._reason!;

    public DomainSuspensionAuditType Type { get; private set; }

    public static DomainSuspensionAudit CreateSuspension(
        DateTime whenHappened,
        DomainSuspensionReason reason, string? notes = null)
    {
        return new DomainSuspensionAudit(whenHappened, DomainSuspensionAuditType.Suspend, reason, notes);
    }

    public static DomainSuspensionAudit CreateUnsuspension(DateTime whenHappened)
    {
        return new DomainSuspensionAudit(whenHappened, DomainSuspensionAuditType.Unsuspend);
    }
}