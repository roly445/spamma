using Spamma.Modules.DomainManagement.Client.Contracts;

namespace Spamma.Modules.DomainManagement.Domain.DomainAggregate;

internal class DomainSuspensionAudit
{
    private readonly DomainSuspensionReason? _reason;

    private readonly string? _notes;

    private DomainSuspensionAudit(
        DateTime happenedAt, DomainSuspensionAuditType type,
        DomainSuspensionReason? reason = null, string? notes = null)
    {
        this.HappenedAt = happenedAt;
        this.Type = type;
        this._reason = reason;
        this._notes = notes;
    }

    internal DateTime HappenedAt { get; private set; }

    internal string? Notes =>
        this.Type == DomainSuspensionAuditType.Unsuspend ?
            throw new InvalidOperationException("Notes is not applicable for unsuspension.") :
            this._notes;

    internal DomainSuspensionReason Reason =>
        this.Type == DomainSuspensionAuditType.Unsuspend ?
            throw new InvalidOperationException("Notes is not applicable for unsuspension.") :
            (DomainSuspensionReason)this._reason!;

    internal DomainSuspensionAuditType Type { get; private set; }

    internal static DomainSuspensionAudit CreateSuspension(
        DateTime happenedAt,
        DomainSuspensionReason reason, string? notes = null)
    {
        return new DomainSuspensionAudit(happenedAt, DomainSuspensionAuditType.Suspend, reason, notes);
    }

    internal static DomainSuspensionAudit CreateUnsuspension(DateTime happenedAt)
    {
        return new DomainSuspensionAudit(happenedAt, DomainSuspensionAuditType.Unsuspend);
    }
}