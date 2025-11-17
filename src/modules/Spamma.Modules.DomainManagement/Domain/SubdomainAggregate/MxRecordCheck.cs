using Spamma.Modules.DomainManagement.Client.Contracts;

namespace Spamma.Modules.DomainManagement.Domain.SubdomainAggregate;

public class MxRecordCheck(DateTime lastCheckedAt, MxStatus mxStatus)
{
    public DateTime LastCheckedAt { get; private set; } = lastCheckedAt;

    public MxStatus MxStatus { get; private set; } = mxStatus;
}