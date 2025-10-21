using Spamma.Modules.DomainManagement.Client.Contracts;

namespace Spamma.Modules.DomainManagement.Domain.SubdomainAggregate;

public class MxRecordCheck
{
    public MxRecordCheck(DateTime whenChecked, MxStatus mxStatus)
    {
        this.WhenChecked = whenChecked;
        this.MxStatus = mxStatus;
    }

    public DateTime WhenChecked { get; private set; }

    public MxStatus MxStatus { get; private set; }
}