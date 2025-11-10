namespace Spamma.Modules.UserManagement.Domain.PasskeyAggregate.Events;

public record PasskeyAuthenticated(uint NewSignCount, DateTime UsedAt);