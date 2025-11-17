using BluQube.Commands;
using ResultMonad;
using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.DomainManagement.Client.Contracts;
using Spamma.Modules.DomainManagement.Domain.SubdomainAggregate.Events;

namespace Spamma.Modules.DomainManagement.Domain.SubdomainAggregate;

/// <summary>
/// Business Logic for Subdomain Aggregate.
/// </summary>
public partial class Subdomain : AggregateRoot
{
    private readonly List<ModerationUser> _moderationUsers = new();
    private readonly List<Viewer> _viewers = new();
    private readonly List<SubdomainSuspensionAudit> _suspensionAudits = new();
    private readonly List<MxRecordCheck> _mxRecordChecks = new();

    public override Guid Id { get; protected set; }

    internal DateTime CreatedAt { get; private set; }

    internal bool IsSuspended { get; private set; }

    internal Guid DomainId { get; private set; }

    internal string Name { get; private set; } = string.Empty;

    internal string? Description { get; private set; }

    internal static Result<Subdomain, BluQubeErrorData> Create(Guid subdomainId, Guid domainId, string name, string? description, DateTime createdAt)
    {
        var @event = new SubdomainCreated(subdomainId, domainId, name, createdAt, description);
        var subdomain = new Subdomain();
        subdomain.RaiseEvent(@event);
        return Result.Ok<Subdomain, BluQubeErrorData>(subdomain);
    }

    internal ResultWithError<BluQubeErrorData> Suspend(SubdomainSuspensionReason reason, string? notes, DateTime suspendedAt)
    {
        if (this.IsSuspended)
        {
            return ResultWithError.Fail(new BluQubeErrorData(
                DomainManagementErrorCodes.AlreadySuspended, $"Subdomain with ID {this.Id} is already suspended"));
        }

        var @event = new SubdomainSuspended(reason, notes, suspendedAt);
        this.RaiseEvent(@event);
        return ResultWithError.Ok<BluQubeErrorData>();
    }

    internal ResultWithError<BluQubeErrorData> Unsuspend(DateTime unsuspendedAt)
    {
        if (!this.IsSuspended)
        {
            return ResultWithError.Fail(new BluQubeErrorData(
                DomainManagementErrorCodes.NotSuspended, $"Subdomain with ID {this.Id} is not suspended"));
        }

        var @event = new SubdomainUnsuspended(unsuspendedAt);
        this.RaiseEvent(@event);
        return ResultWithError.Ok<BluQubeErrorData>();
    }

    internal ResultWithError<BluQubeErrorData> Update(string? description)
    {
        var @event = new SubdomainUpdated(description);
        this.RaiseEvent(@event);
        return ResultWithError.Ok<BluQubeErrorData>();
    }

    internal ResultWithError<BluQubeErrorData> AddModerationUser(Guid userId, DateTime addedAt)
    {
        if (this._moderationUsers.Any(x => x.UserId == userId && x.RemovedAt == null))
        {
            return ResultWithError.Fail(new BluQubeErrorData(
                DomainManagementErrorCodes.UserAlreadyModerator, $"User with ID {userId} is already a moderator for the subdomain {this.Id}"));
        }

        var @event = new ModerationUserAdded(userId, addedAt);
        this.RaiseEvent(@event);
        return ResultWithError.Ok<BluQubeErrorData>();
    }

    internal ResultWithError<BluQubeErrorData> RemoveModerationUser(Guid userId, DateTime removedAt)
    {
        if (!this._moderationUsers.Any(x => x.UserId == userId && x.RemovedAt == null))
        {
            return ResultWithError.Fail(new BluQubeErrorData(
                DomainManagementErrorCodes.UserNotModerator, $"User with ID {userId} is not a moderator for the subdomain {this.Id}"));
        }

        var @event = new ModerationUserRemoved(userId, removedAt);
        this.RaiseEvent(@event);
        return ResultWithError.Ok<BluQubeErrorData>();
    }

    internal ResultWithError<BluQubeErrorData> AddViewer(Guid userId, DateTime addedAt)
    {
        if (this._viewers.Any(x => x.UserId == userId && x.RemovedAt == null))
        {
            return ResultWithError.Fail(new BluQubeErrorData(
                DomainManagementErrorCodes.UserAlreadyViewer, $"User with ID {userId} is already a viewer for the subdomain {this.Id}"));
        }

        var @event = new ViewerAdded(userId, addedAt);
        this.RaiseEvent(@event);
        return ResultWithError.Ok<BluQubeErrorData>();
    }

    internal ResultWithError<BluQubeErrorData> RemoveViewer(Guid userId, DateTime removedAt)
    {
        if (!this._viewers.Any(x => x.UserId == userId && x.RemovedAt == null))
        {
            return ResultWithError.Fail(new BluQubeErrorData(
                DomainManagementErrorCodes.UserNotViewer, $"User with ID {userId} is not a viewer for the subdomain {this.Id}"));
        }

        var @event = new ViewerRemoved(userId, removedAt);
        this.RaiseEvent(@event);
        return ResultWithError.Ok<BluQubeErrorData>();
    }

    internal ResultWithError<BluQubeErrorData> LogMxRecordCheck(MxStatus mxStatus, DateTime lastCheckedAt)
    {
        var @event = new MxRecordChecked(lastCheckedAt, mxStatus);
        this.RaiseEvent(@event);
        return ResultWithError.Ok<BluQubeErrorData>();
    }
}