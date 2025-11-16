using BluQube.Commands;
using ResultMonad;
using Spamma.Modules.Common.Client;
using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.DomainManagement.Client.Contracts;
using Spamma.Modules.DomainManagement.Domain.ChaosAddressAggregate.Events;

namespace Spamma.Modules.DomainManagement.Domain.ChaosAddressAggregate;

public partial class ChaosAddress : AggregateRoot
{
    private readonly List<ChaosAddressSuspensionAudit> _suspensionAudits = new();

    public override Guid Id { get; protected set; }

    public Guid DomainId { get; private set; }

    public Guid SubdomainId { get; private set; }

    public string LocalPart { get; private set; } = string.Empty;

    public SmtpResponseCode ConfiguredSmtpCode { get; private set; }

    public bool Enabled { get; private set; }

    public int TotalReceived { get; private set; }

    public DateTimeOffset? LastReceivedAt { get; private set; }

    internal IReadOnlyList<ChaosAddressSuspensionAudit> SuspensionAudits => this._suspensionAudits;

    public static Result<ChaosAddress, BluQubeErrorData> Create(
        Guid id,
        Guid domainId,
        Guid subdomainId,
        string localPart,
        SmtpResponseCode smtpCode,
        DateTime whenCreated)
    {
        var @event = new ChaosAddressCreated(id, domainId, subdomainId, localPart, smtpCode, whenCreated);
        var aggregate = new ChaosAddress();
        aggregate.RaiseEvent(@event);
        return Result.Ok<ChaosAddress, BluQubeErrorData>(aggregate);
    }

    public ResultWithError<BluQubeErrorData> Enable(DateTime when)
    {
        if (this.Enabled)
        {
            return ResultWithError.Fail(new BluQubeErrorData(
                DomainManagementErrorCodes.AlreadyEnabled,
                $"Chaos address {this.Id} is already enabled"));
        }

        var @event = new ChaosAddressEnabled(when);
        this.RaiseEvent(@event);
        return ResultWithError.Ok<BluQubeErrorData>();
    }

    public ResultWithError<BluQubeErrorData> Disable(DateTime when)
    {
        if (!this.Enabled)
        {
            return ResultWithError.Fail(new BluQubeErrorData(
                DomainManagementErrorCodes.AlreadyDisabled,
                $"Chaos address {this.Id} is already disabled"));
        }

        var @event = new ChaosAddressDisabled(when);
        this.RaiseEvent(@event);
        return ResultWithError.Ok<BluQubeErrorData>();
    }

    public ResultWithError<BluQubeErrorData> RecordReceive(DateTimeOffset when)
    {
        var @event = new ChaosAddressReceived(when);
        this.RaiseEvent(@event);
        return ResultWithError.Ok<BluQubeErrorData>();
    }

    public ResultWithError<BluQubeErrorData> Delete(DateTime when)
    {
        var @event = new ChaosAddressDeleted(when);
        this.RaiseEvent(@event);
        return ResultWithError.Ok<BluQubeErrorData>();
    }
}