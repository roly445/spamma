using BluQube.Commands;
using ResultMonad;
using Spamma.Modules.Common.Client;
using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.DomainManagement.Client.Contracts;
using Spamma.Modules.DomainManagement.Domain.ChaosAddressAggregate.Events;

namespace Spamma.Modules.DomainManagement.Domain.ChaosAddressAggregate;

/// <summary>
/// Business logic for Chaos Address entity.
/// </summary>
internal partial class ChaosAddress : AggregateRoot
{
    private readonly List<ChaosAddressSuspensionAudit> _suspensionAudits = new();
    private DateTimeOffset? _lastReceivedAt;

    private ChaosAddress()
    {
    }

    public override Guid Id { get; protected set; }

    internal Guid DomainId { get; private set; }

    internal Guid SubdomainId { get; private set; }

    internal string LocalPart { get; private set; } = string.Empty;

    internal SmtpResponseCode ConfiguredSmtpCode { get; private set; }

    internal bool Enabled { get; private set; }

    internal int TotalReceived { get; private set; }

    internal DateTimeOffset LastReceivedAt =>
        this._lastReceivedAt ??
        throw new InvalidOperationException("No emails have been received yet. Check TotalReceived before accessing this property.");

    internal IReadOnlyList<ChaosAddressSuspensionAudit> SuspensionAudits => this._suspensionAudits;

    internal static Result<ChaosAddress, BluQubeErrorData> Create(
        Guid id,
        Guid domainId,
        Guid subdomainId,
        string localPart,
        SmtpResponseCode smtpCode,
        DateTime createdAt)
    {
        var @event = new ChaosAddressCreated(id, domainId, subdomainId, localPart, smtpCode, createdAt);
        var aggregate = new ChaosAddress();
        aggregate.RaiseEvent(@event);
        return Result.Ok<ChaosAddress, BluQubeErrorData>(aggregate);
    }

    internal ResultWithError<BluQubeErrorData> Enable(DateTime enabledAt)
    {
        if (this.Enabled)
        {
            return ResultWithError.Fail(new BluQubeErrorData(
                DomainManagementErrorCodes.AlreadyEnabled,
                $"Chaos address {this.Id} is already enabled"));
        }

        var @event = new ChaosAddressEnabled(enabledAt);
        this.RaiseEvent(@event);
        return ResultWithError.Ok<BluQubeErrorData>();
    }

    internal ResultWithError<BluQubeErrorData> Disable(DateTime disabledAt)
    {
        if (!this.Enabled)
        {
            return ResultWithError.Fail(new BluQubeErrorData(
                DomainManagementErrorCodes.AlreadyDisabled,
                $"Chaos address {this.Id} is already disabled"));
        }

        var @event = new ChaosAddressDisabled(disabledAt);
        this.RaiseEvent(@event);
        return ResultWithError.Ok<BluQubeErrorData>();
    }

    internal ResultWithError<BluQubeErrorData> RecordReceive(DateTimeOffset receivedAt)
    {
        var @event = new ChaosAddressReceived(receivedAt);
        this.RaiseEvent(@event);
        return ResultWithError.Ok<BluQubeErrorData>();
    }

    internal ResultWithError<BluQubeErrorData> Delete(DateTime deletedAt)
    {
        var @event = new ChaosAddressDeleted(deletedAt);
        this.RaiseEvent(@event);
        return ResultWithError.Ok<BluQubeErrorData>();
    }
}