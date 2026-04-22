namespace Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

public class CatchAllSenderAddressLookup
{
    public Guid Id { get; set; }

    public string SenderAddress { get; set; } = string.Empty;

    public IReadOnlyList<Guid> AssignedUserIds { get; set; } = [];

    public bool IsRemoved { get; set; }

    public DateTimeOffset AddedAt { get; set; }
}
