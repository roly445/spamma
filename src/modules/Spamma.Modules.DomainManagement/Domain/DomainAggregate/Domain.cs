using BluQube.Commands;
using ResultMonad;
using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.DomainManagement.Client.Contracts;
using Spamma.Modules.DomainManagement.Domain.DomainAggregate.Events;

namespace Spamma.Modules.DomainManagement.Domain.DomainAggregate;

/// <summary>
/// Business logic for the Domain aggregate.
/// </summary>
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

    internal DateTime? VerifiedAt { get; private set; }

    internal DateTime CreatedAt { get; private set; }

    internal string VerificationToken { get; private set; } = string.Empty;

    internal bool IsSuspended { get; private set; }

    internal IReadOnlyList<DomainSuspensionAudit> SuspensionAudits => this._suspensionAudits;

    internal static Result<Domain, BluQubeErrorData> Create(Guid domainId, string name, string? primaryContactEmail, string? description, DateTime createdAt)
    {
        var domain = new Domain();
        var @event = new DomainCreated(domainId, name, primaryContactEmail, description, $"{Guid.NewGuid():N}", createdAt);
        domain.RaiseEvent(@event);

        return Result.Ok<Domain, BluQubeErrorData>(domain);
    }

    internal ResultWithError<BluQubeErrorData> ChangeDetails(string? primaryContactEmail, string? description)
    {
        var @event = new DetailsUpdated(primaryContactEmail, description);
        this.RaiseEvent(@event);
        return ResultWithError.Ok<BluQubeErrorData>();
    }

    internal ResultWithError<BluQubeErrorData> MarkAsVerified(DateTime verifiedAt)
    {
        if (this.VerifiedAt.HasValue)
        {
            return ResultWithError.Fail(new BluQubeErrorData(
                DomainManagementErrorCodes.AlreadyVerified, $"Domain with ID {this.Id} is already verified"));
        }

        var @event = new DomainVerified(verifiedAt);
        this.RaiseEvent(@event);
        return ResultWithError.Ok<BluQubeErrorData>();
    }

    internal ResultWithError<BluQubeErrorData> Suspend(
        DomainSuspensionReason reason, string? notes, DateTime suspendedAt)
    {
        if (this.IsSuspended)
        {
            return ResultWithError.Fail(new BluQubeErrorData(
                DomainManagementErrorCodes.AlreadySuspended, $"Domain with ID {this.Id} is already suspended"));
        }

        var @event = new DomainSuspended(reason, notes, suspendedAt);
        this.RaiseEvent(@event);
        return ResultWithError.Ok<BluQubeErrorData>();
    }

    internal ResultWithError<BluQubeErrorData> Unsuspend(DateTime unsuspendedAt)
    {
        if (!this.IsSuspended)
        {
            return ResultWithError.Fail(new BluQubeErrorData(
                DomainManagementErrorCodes.NotSuspended, $"Domain with ID {this.Id} is not suspended"));
        }

        var @event = new DomainUnsuspended(unsuspendedAt);
        this.RaiseEvent(@event);
        return ResultWithError.Ok<BluQubeErrorData>();
    }

    internal ResultWithError<BluQubeErrorData> AddModerationUser(Guid userId, DateTime addedAt)
    {
        if (this._moderationUsers.Any(x => x.UserId == userId && x.RemovedAt == null))
        {
            return ResultWithError.Fail(new BluQubeErrorData(
                DomainManagementErrorCodes.UserAlreadyModerator, $"User with ID {userId} is already a moderator for domain {this.Id}"));
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
                DomainManagementErrorCodes.UserNotModerator, $"User with ID {userId} is not a moderator for domain {this.Id}"));
        }

        var @event = new ModerationUserRemoved(userId, removedAt);
        this.RaiseEvent(@event);
        return ResultWithError.Ok<BluQubeErrorData>();
    }
}