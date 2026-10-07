using System.Security.Cryptography;
using System.Text;

namespace Spamma.Modules.EmailInbox.Application.CommandHandlers.Campaign;

internal static class CampaignIdentity
{
    internal static Guid FromValue(Guid subdomainId, string campaignValue)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{subdomainId}:{campaignValue}"));
        return new Guid(hash.AsSpan(0, 16));
    }
}
