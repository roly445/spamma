namespace Spamma.Modules.DomainManagement.Infrastructure.ReadModels;

public class Viewer
{
    public Guid UserId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; }
}