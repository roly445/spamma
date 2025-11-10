using Marten;
using MaybeMonad;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common.Infrastructure;
using Spamma.Modules.UserManagement.Application.Repositories;
using Spamma.Modules.UserManagement.Domain.PasskeyAggregate;
using Spamma.Modules.UserManagement.Infrastructure.ReadModels;

namespace Spamma.Modules.UserManagement.Infrastructure.Repositories;

public class PasskeyRepository(IDocumentSession session, ILogger<PasskeyRepository> logger) : GenericRepository<Passkey>(session), IPasskeyRepository
{
    private readonly IDocumentSession _session = session;

    public async Task<Maybe<Passkey>> GetByCredentialIdAsync(byte[] credentialId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Looking up passkey by credential ID: {CredentialIdHex} (length: {Length})",
            string.Join(" ", credentialId.Select(b => b.ToString("X2"))),
            credentialId.Length);

        try
        {
            // Convert credentialId to base64 for JSON comparison to avoid byte[] serialization issues
            var credentialIdBase64 = Convert.ToBase64String(credentialId);

            // Query the PasskeyLookup read model using base64 string comparison
            // This avoids JSON byte array comparison issues that cause PostgreSQL 22P02 errors
            var lookup = await this._session
                .Query<PasskeyLookup>()
                .Where(p => !p.IsRevoked)
                .ToListAsync(cancellationToken);

            // Filter in memory to avoid JSON byte[] comparison in database
            lookup = lookup.Where(p => Convert.ToBase64String(p.CredentialId) == credentialIdBase64).ToList();
            var foundLookup = lookup.FirstOrDefault();

            if (foundLookup == null)
            {
                logger.LogWarning("No passkey lookup found for credential ID");

                // Let's also check if there are any passkeys at all
                var allPasskeys = await this._session
                    .Query<PasskeyLookup>()
                    .Take(5)
                    .ToListAsync(cancellationToken);

                logger.LogInformation("Found {Count} passkey lookup records in database", allPasskeys.Count);
                foreach (var pk in allPasskeys)
                {
                    logger.LogInformation(
                        "Existing passkey: ID={PasskeyId}, CredentialId={CredentialIdHex}, IsRevoked={IsRevoked}",
                        pk.Id,
                        string.Join(" ", pk.CredentialId.Select(b => b.ToString("X2"))),
                        pk.IsRevoked);
                }

                return Maybe<Passkey>.Nothing;
            }

            logger.LogInformation("Found passkey lookup: ID={PasskeyId}, loading aggregate", foundLookup.Id);

            // Load the aggregate by its ID using the base class method (event sourcing)
            return await this.GetByIdAsync(foundLookup.Id, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during passkey lookup query - likely malformed JSON in database");

            // Try to get raw data to see what's wrong
            try
            {
                var rawCount = await this._session
                    .Query<PasskeyLookup>()
                    .CountAsync(cancellationToken);

                logger.LogInformation("Total PasskeyLookup records in database: {Count}", rawCount);
            }
            catch (Exception countEx)
            {
                logger.LogError(countEx, "Even basic count query failed - database schema issue");
            }

            return Maybe<Passkey>.Nothing;
        }
    }
}