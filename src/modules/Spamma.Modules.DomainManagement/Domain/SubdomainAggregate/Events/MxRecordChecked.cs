using Spamma.Modules.DomainManagement.Client.Contracts;

namespace Spamma.Modules.DomainManagement.Domain.SubdomainAggregate.Events;

public record MxRecordChecked(DateTime LastCheckedAt, MxStatus MxStatus);