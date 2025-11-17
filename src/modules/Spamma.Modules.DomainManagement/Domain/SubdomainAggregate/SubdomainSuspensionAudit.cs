using Spamma.Modules.DomainManagement.Client.Contracts;

namespace Spamma.Modules.DomainManagement.Domain.SubdomainAggregate;

public class SubdomainSuspensionAudit
{
    private readonly SubdomainSuspensionReason? _reason;

    private readonly string? _notes;

    private SubdomainSuspensionAudit(
        DateTime happenedAt, SubdomainSuspensionAuditType type,
        SubdomainSuspensionReason? reason = null, string? notes = null)
    {
        this.HappenedAt = happenedAt;
        this.Type = type;
        this._reason = reason;
        this._notes = notes;
    }

    internal DateTime HappenedAt { get; private set; }

    internal string? Notes =>
        this.Type == SubdomainSuspensionAuditType.Unsuspend ?
            throw new InvalidOperationException("Notes is not applicable for unsuspension.") :
            this._notes;

    internal SubdomainSuspensionReason Reason =>
        this.Type == SubdomainSuspensionAuditType.Unsuspend ?
            throw new InvalidOperationException("Notes is not applicable for unsuspension.") :
            (SubdomainSuspensionReason)this._reason!;

    internal SubdomainSuspensionAuditType Type { get; private set; }

    internal static SubdomainSuspensionAudit CreateSuspension(
        DateTime happenedAt,
        SubdomainSuspensionReason reason, string? notes = null)
    {
        return new SubdomainSuspensionAudit(happenedAt, SubdomainSuspensionAuditType.Suspend, reason, notes);
    }

    internal static SubdomainSuspensionAudit CreateUnsuspension(DateTime happenedAt)
    {
        return new SubdomainSuspensionAudit(happenedAt, SubdomainSuspensionAuditType.Unsuspend);
    }
}