namespace Spamma.Modules.DomainManagement.Domain.DomainAggregate.Events;

public record DomainCreated(Guid DomainId, string Name, string? PrimaryContactEmail, string? Description, string VerificationToken, DateTime WhenCreated);