namespace Spamma.Modules.UserManagement.Domain.PasskeyAggregate.Events;

public record PasskeyRevoked(Guid RevokedByUserId, DateTime RevokedAt);