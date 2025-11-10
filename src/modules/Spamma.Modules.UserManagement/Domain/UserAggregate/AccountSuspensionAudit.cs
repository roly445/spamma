using Spamma.Modules.UserManagement.Client.Contracts;

namespace Spamma.Modules.UserManagement.Domain.UserAggregate;

public class AccountSuspensionAudit
{
    private readonly AccountSuspensionReason _reason;
    private readonly string? _notes;

    private AccountSuspensionAudit(DateTime whenHappened, AccountSuspensionAuditType type, AccountSuspensionReason reason = AccountSuspensionReason.Unknown,  string? notes = null)
    {
        this.WhenHappened = whenHappened;
        this.Type = type;
        this._reason = reason;
        this._notes = notes;
    }

    public DateTime WhenHappened { get; private set; }

    public string Notes => this.Type == AccountSuspensionAuditType.Unsuspend ? throw new InvalidOperationException("Notes are not applicable for unsuspension.") : this._notes!;

    public AccountSuspensionReason Reason => this.Type == AccountSuspensionAuditType.Unsuspend ? throw new InvalidOperationException("Reason is not applicable for unsuspension.") : this._reason;

    public AccountSuspensionAuditType Type { get; private set; }

    public static AccountSuspensionAudit CreateSuspension(DateTime whenHappened, AccountSuspensionReason reason, string? notes)
    {
        return new AccountSuspensionAudit(whenHappened, AccountSuspensionAuditType.Suspend, reason, notes);
    }

    public static AccountSuspensionAudit CreateUnsuspension(DateTime whenHappened)
    {
        return new AccountSuspensionAudit(whenHappened, AccountSuspensionAuditType.Unsuspend);
    }
}