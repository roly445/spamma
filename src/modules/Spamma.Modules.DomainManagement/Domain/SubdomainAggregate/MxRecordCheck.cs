using Spamma.Modules.DomainManagement.Client.Contracts;

namespace Spamma.Modules.DomainManagement.Domain.SubdomainAggregate;

internal class MxRecordCheck(DateTime lastCheckedAt, MxStatus mxStatus)
{
    internal DateTime LastCheckedAt { get; private set; } = lastCheckedAt;

    internal MxStatus MxStatus { get; private set; } = mxStatus;
}