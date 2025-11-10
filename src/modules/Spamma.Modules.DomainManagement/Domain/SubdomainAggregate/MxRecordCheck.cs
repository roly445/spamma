using Spamma.Modules.DomainManagement.Client.Contracts;

namespace Spamma.Modules.DomainManagement.Domain.SubdomainAggregate;

public class MxRecordCheck(DateTime whenChecked, MxStatus mxStatus)
{
    public DateTime WhenChecked { get; private set; } = whenChecked;

    public MxStatus MxStatus { get; private set; } = mxStatus;
}