# quickstart.md

This quickstart outlines how to run and test the Email Campaigns feature locally.

1. Start infrastructure (Postgres, Redis, Mailhog) via docker-compose as per repo README.
2. Ensure backend builds: `dotnet build Spamma.sln` (run from repo root).
3. Run the app and navigate to Campaigns page in the Blazor client after seed/setup.
4. To test campaign capture:
   - Send an email to example subdomain with header `x-spamma-camp: promo-1` to SMTP port (1025).
   - Confirm campaign counts are updated in the Campaigns read model (may require manual refresh if read-model async).

Export testing:
- Use UI export controls to request CSV or JSON; small exports (<=10k rows) will be synchronous and download directly.
