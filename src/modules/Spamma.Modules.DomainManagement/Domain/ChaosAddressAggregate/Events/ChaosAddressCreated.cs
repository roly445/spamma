using Spamma.Modules.Common;

namespace Spamma.Modules.DomainManagement.Domain.ChaosAddressAggregate.Events;

public record ChaosAddressCreated(Guid Id, Guid DomainId, Guid SubdomainId, string LocalPart, SmtpResponseCode ConfiguredSmtpCode, DateTime CreatedAt);
