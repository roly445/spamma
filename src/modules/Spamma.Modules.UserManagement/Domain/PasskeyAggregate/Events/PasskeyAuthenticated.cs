namespace Spamma.Modules.UserManagement.Domain.PasskeyAggregate.Events;

public record PasskeyAuthenticated(Guid UserId, uint NewSignCount, DateTime UsedAt);