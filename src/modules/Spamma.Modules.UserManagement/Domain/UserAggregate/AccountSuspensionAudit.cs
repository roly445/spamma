using Spamma.Modules.UserManagement.Client.Contracts;

namespace Spamma.Modules.UserManagement.Domain.UserAggregate;

public class AccountSuspensionAudit
{
    private readonly AccountSuspensionReason _reason;
    private readonly string? _notes;

    private AccountSuspensionAudit(DateTime happenedAt, AccountSuspensionAuditType type, AccountSuspensionReason reason = AccountSuspensionReason.Unknown,  string? notes = null)
    {
        this.HappenedAt = happenedAt;
        this.Type = type;
        this._reason = reason;
        this._notes = notes;
    }

    internal DateTime HappenedAt { get; private set; }

    internal string Notes => this.Type == AccountSuspensionAuditType.Unsuspend ? throw new InvalidOperationException("Notes are not applicable for unsuspension.") : this._notes!;

    internal AccountSuspensionReason Reason => this.Type == AccountSuspensionAuditType.Unsuspend ? throw new InvalidOperationException("Reason is not applicable for unsuspension.") : this._reason;

    internal AccountSuspensionAuditType Type { get; private set; }

    internal static AccountSuspensionAudit CreateSuspension(DateTime happenedAt, AccountSuspensionReason reason, string? notes)
    {
        return new AccountSuspensionAudit(happenedAt, AccountSuspensionAuditType.Suspend, reason, notes);
    }

    internal static AccountSuspensionAudit CreateUnsuspension(DateTime happenedAt)
    {
        return new AccountSuspensionAudit(happenedAt, AccountSuspensionAuditType.Unsuspend);
    }
}