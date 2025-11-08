# Rebuilding Projections

When you need to repopulate the Marten event store projections (read models), you have several options:

## Option 1: Using the Standalone Tool (Recommended)

The `ProjectionsRebuildTool` is a dedicated command-line utility that clears projection data without starting the web application:

```bash
# From the root directory
cd tools/ProjectionsRebuildTool
dotnet run
```

**What it does:**

- Connects directly to PostgreSQL using settings from `appsettings.json` in the tool directory
- Clears all projection/read model documents from the database
- Displays a success message and exits immediately
- The application then automatically rebuilds all projections when you start it normally

**Note:** Make sure your database credentials in `tools/ProjectionsRebuildTool/appsettings.json` match your PostgreSQL setup.

## Option 2: Using Docker Compose with PostgreSQL

Connect directly to the PostgreSQL database and clear projection tables:

```bash
# Connect to PostgreSQL in Docker
docker-compose exec postgres psql -U postgres -d spamma -c "DELETE FROM mt_doc_userlookup; DELETE FROM mt_doc_emaillookup; DELETE FROM mt_doc_domainsummary;"
```

Then restart the application to rebuild projections.

## Option 3: Manual SQL Reset (Development Only)

If you have direct PostgreSQL access:

```sql
-- Connect to the spamma database
DELETE FROM mt_doc_userlookup;
DELETE FROM mt_doc_emaillookup;
DELETE FROM mt_doc_campaignsummary;
DELETE FROM mt_doc_domainsummary;
DELETE FROM mt_doc_subdomainsummary;
DELETE FROM mt_doc_passkeylookup;
DELETE FROM mt_doc_chaosaddresslookup;
```

Then restart the application to rebuild from the event store.

## What Gets Rebuilt

The application automatically rebuilds these projections on startup:

1. **UserLookupProjection** - User account metadata and status
2. **PasskeyProjection** - WebAuthn passkey summaries
3. **DomainLookupProjection** - Domain configuration and moderation
4. **SubdomainLookupProjection** - Subdomain configuration
5. **ChaosAddressLookupProjection** - Chaos address metadata
6. **EmailLookupProjection** - Email inbox metadata and favorites
7. **CampaignSummaryProjection** - Email campaign summaries

## Troubleshooting

### Projections not rebuilding

Ensure:

1. The database is running (check `docker-compose ps`)
2. You've restarted the application after clearing projections
3. Check logs for any errors during startup with `ApplyAllDatabaseChangesOnStartup()`

### Database connection issues

Verify your connection strings in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=5432;Database=spamma;User Id=postgres;Password=password;",
    "Redis": "localhost:6379"
  }
}
```

## Performance Notes

- Rebuilding projections is I/O bound and depends on the number of events in your event store
- Large event histories may take several seconds to rebuild
- This is a safe operation - it doesn't affect the event store itself, only read models
- No user data is lost during projection rebuilds

