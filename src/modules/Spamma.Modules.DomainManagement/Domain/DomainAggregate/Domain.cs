using BluQube.Commands;
using ResultMonad;
using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.DomainManagement.Client.Contracts;
using Spamma.Modules.DomainManagement.Domain.DomainAggregate.Events;

namespace Spamma.Modules.DomainManagement.Domain.DomainAggregate;

public partial class Domain : AggregateRoot
{
    private readonly List<ModerationUser> _moderationUsers = new();
    private readonly List<DomainSuspensionAudit> _suspensionAudits = new();

    private Domain()
    {
    }

    public override Guid Id { get; protected set; }

    internal string Name { get; private set; } = string.Empty;

    internal string? PrimaryContactEmail { get; private set; }

    internal string? Description { get; private set; }

    internal DateTime? WhenVerified { get; private set; }

    internal DateTime WhenCreated { get; private set; }

    internal string VerificationToken { get; private set; } = string.Empty;

    internal bool IsSuspended
    {
        get
        {
            var attempt = this._suspensionAudits.OrderBy(x => x.WhenHappened).LastOrDefault();
            if (attempt is null)
            {
                return false;
            }

            return attempt.Type == DomainSuspensionAuditType.Suspend;
        }
    }

    internal IReadOnlyList<DomainSuspensionAudit> SuspensionAudits => this._suspensionAudits;

    internal static Result<Domain, BluQubeErrorData> Create(Guid domainId, string name, string? primaryContactEmail, string? description, DateTime whenCreated)
    {
        var domain = new Domain();
        var @event = new DomainCreated(domainId, name, primaryContactEmail, description, $"{Guid.NewGuid():N}", whenCreated);
        domain.RaiseEvent(@event);

        return Result.Ok<Domain, BluQubeErrorData>(domain);
    }

    internal ResultWithError<BluQubeErrorData> ChangeDetails(string? primaryContactEmail, string? description)
    {
        var @event = new DetailsUpdated(primaryContactEmail, description);
        this.RaiseEvent(@event);
        return ResultWithError.Ok<BluQubeErrorData>();
    }

    internal ResultWithError<BluQubeErrorData> MarkAsVerified(DateTime whenVerified)
    {
        if (this.WhenVerified.HasValue)
        {
            return ResultWithError.Fail(new BluQubeErrorData(DomainManagementErrorCodes.AlreadyVerified, $"Domain with ID {this.Id} is already verified"));
        }

        var @event = new DomainVerified(whenVerified);
        this.RaiseEvent(@event);
        return ResultWithError.Ok<BluQubeErrorData>();
    }

    internal ResultWithError<BluQubeErrorData> Suspend(DomainSuspensionReason reason, string? notes, DateTime whenSuspended)
    {
        if (this.IsSuspended)
        {
            return ResultWithError.Fail(new BluQubeErrorData(DomainManagementErrorCodes.AlreadySuspended, $"Domain with ID {this.Id} is already suspended"));
        }

        var @event = new DomainSuspended(reason, notes, whenSuspended);
        this.RaiseEvent(@event);
        return ResultWithError.Ok<BluQubeErrorData>();
    }

    internal ResultWithError<BluQubeErrorData> Unsuspend(DateTime whenUnsuspended)
    {
        if (!this.IsSuspended)
        {
            return ResultWithError.Fail(new BluQubeErrorData(DomainManagementErrorCodes.NotSuspended, $"Domain with ID {this.Id} is not suspended"));
        }

        var @event = new DomainUnsuspended(whenUnsuspended);
        this.RaiseEvent(@event);
        return ResultWithError.Ok<BluQubeErrorData>();
    }

    internal ResultWithError<BluQubeErrorData> AddModerationUser(Guid userId, DateTime whenAdded)
    {
        if (this._moderationUsers.Any(x => x.UserId == userId && x.WhenRemoved == null))
        {
            return ResultWithError.Fail(new BluQubeErrorData(DomainManagementErrorCodes.UserAlreadyModerator, $"User with ID {userId} is already a moderator for domain {this.Id}"));
        }

        var @event = new ModerationUserAdded(userId, whenAdded);
        this.RaiseEvent(@event);
        return ResultWithError.Ok<BluQubeErrorData>();
    }

    internal ResultWithError<BluQubeErrorData> RemoveModerationUser(Guid userId, DateTime whenRemoved)
    {
        if (!this._moderationUsers.Any(x => x.UserId == userId && x.WhenRemoved == null))
        {
            return ResultWithError.Fail(new BluQubeErrorData(DomainManagementErrorCodes.UserNotModerator, $"User with ID {userId} is not a moderator for domain {this.Id}"));
        }

        var @event = new ModerationUserRemoved(userId, whenRemoved);
        this.RaiseEvent(@event);
        return ResultWithError.Ok<BluQubeErrorData>();
    }
}