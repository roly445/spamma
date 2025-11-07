using BluQube.Commands;
using ResultMonad;
using Spamma.Modules.Common.Client;
using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.UserManagement.Client.Contracts;
using Spamma.Modules.UserManagement.Domain.UserAggregate.Events;

namespace Spamma.Modules.UserManagement.Domain.UserAggregate;

public partial class User : AggregateRoot
{
    private readonly List<AuthenticationAttempt> _authenticationAttempts = new();
    private readonly List<AccountSuspensionAudit> _accountSuspensionAudits = new();

    private User()
    {
    }

    public override Guid Id { get; protected set; }

    public string Name { get; private set; } = string.Empty;

    public Guid SecurityStamp { get; private set; }

    public string EmailAddress { get; private set; } = string.Empty;

    public bool IsSuspended { get; private set; }

    public SystemRole SystemRole { get; private set; }

    public IReadOnlyList<AuthenticationAttempt> AuthenticationAttempts => this._authenticationAttempts;

    public IReadOnlyList<AccountSuspensionAudit> AccountSuspensionAudits => this._accountSuspensionAudits;

    public Result<AuthenticationStarted, BluQubeErrorData> StartAuthentication(DateTime whenStarted)
    {
        if (this.IsSuspended)
        {
            return Result.Fail<AuthenticationStarted, BluQubeErrorData>(new BluQubeErrorData(UserManagementErrorCodes.AccountSuspended, $"User with ID {this.Id} is suspended"));
        }

        var id = Guid.NewGuid();
        var @event = new AuthenticationStarted(id, whenStarted);
        this.RaiseEvent(@event);
        return Result.Ok<AuthenticationStarted, BluQubeErrorData>(@event);
    }

    public Result<bool, BluQubeErrorData> ProcessAuthentication(
        Guid authenticationAttemptId, Guid securityStamp, DateTime whenCompleted, int tokenTimeInMinutes)
    {
        var authenticationAttempt = this._authenticationAttempts.SingleOrDefault(a => a.Id == authenticationAttemptId);
        if (authenticationAttempt is not { WhenCompleted: null } || authenticationAttempt.WhenFailed.HasValue)
        {
            return Result.Fail<bool, BluQubeErrorData>(new BluQubeErrorData(UserManagementErrorCodes.InvalidAuthenticationAttempt, $"Authentication attempt with ID {authenticationAttemptId} is not valid"));
        }

        if (securityStamp != this.SecurityStamp)
        {
            this.RaiseEvent(new AuthenticationFailed(authenticationAttemptId, whenCompleted, Guid.Empty));
            return Result.Ok<bool, BluQubeErrorData>(false);
        }

        var isSuccessful = authenticationAttempt.WhenStarted.AddMinutes(tokenTimeInMinutes) >= whenCompleted;

        this.RaiseEvent(isSuccessful
            ? new AuthenticationCompleted(authenticationAttemptId, whenCompleted, Guid.NewGuid())
            : new AuthenticationFailed(authenticationAttemptId, whenCompleted, Guid.NewGuid()));

        return Result.Ok<bool, BluQubeErrorData>(isSuccessful);
    }

    public ResultWithError<BluQubeErrorData> ChangeDetails(string emailAddress, string name, SystemRole systemRole)
    {
        var @event = new DetailsChanged(emailAddress, name, systemRole);
        this.RaiseEvent(@event);
        return ResultWithError.Ok<BluQubeErrorData>();
    }

    public ResultWithError<BluQubeErrorData> Suspend(AccountSuspensionReason reason, string? notes, DateTime whenSuspended)
    {
        if (this.IsSuspended)
        {
            return ResultWithError.Fail(new BluQubeErrorData(UserManagementErrorCodes.AlreadySuspended, $"User with ID {this.Id} is already suspended"));
        }

        var @event = new AccountSuspended(reason, notes, whenSuspended, Guid.NewGuid());
        this.RaiseEvent(@event);
        return ResultWithError.Ok<BluQubeErrorData>();
    }

    public ResultWithError<BluQubeErrorData> Unsuspend(DateTime whenUnSuspended)
    {
        if (!this.IsSuspended)
        {
            return ResultWithError.Fail<BluQubeErrorData>(new BluQubeErrorData(UserManagementErrorCodes.NotSuspended, $"User with ID {this.Id} is not suspended"));
        }

        var @event = new AccountUnsuspended(whenUnSuspended, Guid.NewGuid());
        this.RaiseEvent(@event);
        return ResultWithError.Ok<BluQubeErrorData>();
    }

    internal static Result<User, BluQubeErrorData> Create(Guid userId, string name, string emailAddress, Guid securityStamp, DateTime whenCreated, SystemRole systemRole)
    {
        var user = new User();
        var @event = new UserCreated(userId, name, emailAddress, securityStamp, whenCreated, systemRole);
        user.RaiseEvent(@event);

        return Result.Ok<User, BluQubeErrorData>(user);
    }
}