namespace Spamma.Modules.DomainManagement.Domain.DomainAggregate.Events;

public record DetailsUpdated(string? PrimaryContactEmail, string? Description);