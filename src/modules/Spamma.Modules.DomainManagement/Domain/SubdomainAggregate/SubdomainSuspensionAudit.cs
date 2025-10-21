using Spamma.Modules.DomainManagement.Client.Contracts;

namespace Spamma.Modules.DomainManagement.Domain.SubdomainAggregate;

public class SubdomainSuspensionAudit
{
    private readonly SubdomainSuspensionReason? _reason;

    private readonly string? _notes;

    private SubdomainSuspensionAudit(
        DateTime whenHappened, SubdomainSuspensionAuditType type,
        SubdomainSuspensionReason? reason = null, string? notes = null)
    {
        this.WhenHappened = whenHappened;
        this.Type = type;
        this._reason = reason;
        this._notes = notes;
    }

    public DateTime WhenHappened { get; private set; }

    public string? Notes =>
        this.Type == SubdomainSuspensionAuditType.Unsuspend ?
            throw new InvalidOperationException("Notes is not applicable for unsuspension.") :
            this._notes;

    public SubdomainSuspensionReason Reason =>
        this.Type == SubdomainSuspensionAuditType.Unsuspend ?
            throw new InvalidOperationException("Notes is not applicable for unsuspension.") :
            (SubdomainSuspensionReason)this._reason!;

    public SubdomainSuspensionAuditType Type { get; private set; }

    public static SubdomainSuspensionAudit CreateSuspension(
        DateTime whenHappened,
        SubdomainSuspensionReason reason, string? notes = null)
    {
        return new SubdomainSuspensionAudit(whenHappened, SubdomainSuspensionAuditType.Suspend, reason, notes);
    }

    public static SubdomainSuspensionAudit CreateUnsuspension(DateTime whenHappened)
    {
        return new SubdomainSuspensionAudit(whenHappened, SubdomainSuspensionAuditType.Unsuspend);
    }
}