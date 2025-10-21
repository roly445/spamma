using BluQube.Commands;
using ResultMonad;
using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.DomainManagement.Client.Contracts;
using Spamma.Modules.DomainManagement.Domain.SubdomainAggregate.Events;

namespace Spamma.Modules.DomainManagement.Domain.SubdomainAggregate;

public partial class Subdomain : AggregateRoot
{
    private readonly List<ModerationUser> _moderationUsers = new();
    private readonly List<Viewer> _viewers = new();
    private readonly List<SubdomainSuspensionAudit> _suspensionAudits = new();
    private readonly List<MxRecordCheck> _mxRecordChecks = new();

    public override Guid Id { get; protected set; }

    public DateTime WhenCreated { get; private set; }

    public bool IsSuspended { get; private set; }

    public Guid DomainId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public static Result<Subdomain, BluQubeErrorData> Create(Guid subdomainId, Guid domainId, string name, string? description, DateTime whenCreated)
    {
        var @event = new SubdomainCreated(subdomainId, domainId, name, whenCreated, description);
        var subdomain = new Subdomain();
        subdomain.RaiseEvent(@event);
        return Result.Ok<Subdomain, BluQubeErrorData>(subdomain);
    }

    public ResultWithError<BluQubeErrorData> Suspend(SubdomainSuspensionReason reason, string? notes, DateTime whenSuspended)
    {
        if (this.IsSuspended)
        {
            return ResultWithError.Fail(new BluQubeErrorData(DomainManagementErrorCodes.AlreadySuspended, $"Subdomain with ID {this.Id} is already suspended"));
        }

        var @event = new SubdomainSuspended(reason, notes, whenSuspended);
        this.RaiseEvent(@event);
        return ResultWithError.Ok<BluQubeErrorData>();
    }

    public ResultWithError<BluQubeErrorData> Unsuspend(DateTime whenUnsuspended)
    {
        if (!this.IsSuspended)
        {
            return ResultWithError.Fail(new BluQubeErrorData(DomainManagementErrorCodes.NotSuspended, $"Subdomain with ID {this.Id} is not suspended"));
        }

        var @event = new SubdomainUnsuspended(whenUnsuspended);
        this.RaiseEvent(@event);
        return ResultWithError.Ok<BluQubeErrorData>();
    }

    public ResultWithError<BluQubeErrorData> Update(string? description)
    {
        var @event = new SubdomainUpdated(description);
        this.RaiseEvent(@event);
        return ResultWithError.Ok<BluQubeErrorData>();
    }

    public ResultWithError<BluQubeErrorData> AddModerationUser(Guid userId, DateTime whenAdded)
    {
        if (this._moderationUsers.Any(x => x.UserId == userId && x.WhenRemoved == null))
        {
            return ResultWithError.Fail(new BluQubeErrorData(DomainManagementErrorCodes.UserAlreadyModerator, $"User with ID {userId} is already a moderator for the subdomain {this.Id}"));
        }

        var @event = new ModerationUserAdded(userId, whenAdded);
        this.RaiseEvent(@event);
        return ResultWithError.Ok<BluQubeErrorData>();
    }

    public ResultWithError<BluQubeErrorData> RemoveModerationUser(Guid userId, DateTime whenRemoved)
    {
        if (!this._moderationUsers.Any(x => x.UserId == userId && x.WhenRemoved == null))
        {
            return ResultWithError.Fail(new BluQubeErrorData(DomainManagementErrorCodes.UserNotModerator, $"User with ID {userId} is not a moderator for the subdomain {this.Id}"));
        }

        var @event = new ModerationUserRemoved(userId, whenRemoved);
        this.RaiseEvent(@event);
        return ResultWithError.Ok<BluQubeErrorData>();
    }

    public ResultWithError<BluQubeErrorData> AddViewer(Guid userId, DateTime whenAdded)
    {
        if (this._viewers.Any(x => x.UserId == userId && x.WhenRemoved == null))
        {
            return ResultWithError.Fail<BluQubeErrorData>(new BluQubeErrorData(DomainManagementErrorCodes.UserAlreadyViewer, $"User with ID {userId} is already a viewer for the subdomain {this.Id}"));
        }

        var @event = new ViewerAdded(userId, whenAdded);
        this.RaiseEvent(@event);
        return ResultWithError.Ok<BluQubeErrorData>();
    }

    public ResultWithError<BluQubeErrorData> RemoveViewer(Guid userId, DateTime whenRemoved)
    {
        if (!this._viewers.Any(x => x.UserId == userId && x.WhenRemoved == null))
        {
            return ResultWithError.Fail(new BluQubeErrorData(DomainManagementErrorCodes.UserNotViewer, $"User with ID {userId} is not a viewer for the subdomain {this.Id}"));
        }

        var @event = new ViewerRemoved(userId, whenRemoved);
        this.RaiseEvent(@event);
        return ResultWithError.Ok<BluQubeErrorData>();
    }

    public ResultWithError<BluQubeErrorData> LogMxRecordCheck(MxStatus mxStatus, DateTime whenChecked)
    {
        var @event = new MxRecordChecked(whenChecked, mxStatus);
        this.RaiseEvent(@event);
        return ResultWithError.Ok<BluQubeErrorData>();
    }
}