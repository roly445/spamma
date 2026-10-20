namespace Spamma.Modules.DomainManagement.Domain.DomainAggregate.Events;

public record DomainCreatedV2(
    Guid DomainId, string Name, string? PrimaryContactEmail, string? Description, string VerificationToken,
    DateTime CreatedAt, Guid? DnsIntegrationId);
