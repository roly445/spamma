namespace Spamma.Modules.DomainManagement.Infrastructure.ReadModels;

public class SubdomainModerator
{
    public Guid UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}