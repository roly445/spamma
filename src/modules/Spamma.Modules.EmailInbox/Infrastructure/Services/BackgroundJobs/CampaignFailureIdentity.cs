using System.Security.Cryptography;
using System.Text;

namespace Spamma.Modules.EmailInbox.Infrastructure.Services.BackgroundJobs;

internal static class CampaignFailureIdentity
{
    // The same raw message sent again to the same recipient and failure class is one affected message.
    // Hashing the MIME content also distinguishes messages that reuse Message-ID with different content.
    // Without any difference in MIME, recipient, or failure class, two deliveries are indistinguishable.
    internal static Guid FromMessage(Stream mimeStream, Guid chaosAddressId, string recipient, int smtpCode)
    {
        mimeStream.Position = 0;
        var contentHash = SHA256.HashData(mimeStream);
        mimeStream.Position = 0;
        var scope = Encoding.UTF8.GetBytes($"{chaosAddressId}:{recipient.Trim().ToLowerInvariant()}:{smtpCode / 100}:");
        var combined = new byte[scope.Length + contentHash.Length];
        scope.CopyTo(combined, 0);
        contentHash.CopyTo(combined, scope.Length);
        return new Guid(SHA256.HashData(combined).AsSpan(0, 16));
    }
}
