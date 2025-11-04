# contracts/

This folder is reserved for API contract artifacts if required. In this project we use BluQube's command/query codegen patterns for Blazor WASM clients, so OpenAPI stubs are not required by default.

Notes:
- Query and command DTOs for the Campaigns feature SHOULD be declared in `Spamma.Modules.*.Client` projects with the appropriate `BluQubeQuery` / `BluQubeCommand` attributes. BluQube tooling will generate the necessary API wiring.
- If downstream integrators require OpenAPI, generate a minimal OpenAPI export from runtime endpoints or add a separate task in `tasks.md`.

Export schema & async job lifecycle (recommended)

- Export row fields (CSV): SubdomainId, CampaignValue, FirstReceivedAt, LastReceivedAt, TotalCaptured, SampleMessageId, SampleSubject, SampleFrom, SampleReceivedAt
- Export JSON payload: array of objects with the above fields; if SampleMessageId is null, sample fields are omitted.

- Async export job flow:
	1. Client POSTs `ExportCampaignDataQuery` → server returns `ExportCampaignDataQueryResult` with a `JobId` and optionally `DownloadUrl` if synchronous.
 2. For large exports the server enqueues an ExportJob and returns `JobId` immediately. Client polls `/api/email-inbox/campaigns/export/status/{JobId}` for progress or the server sends a notification and stores the artifact in the configured IO store.
 3. Export artifacts retained per operator-configured retention policy; access controlled by export authorization rules.

Add tasks to implement job status endpoint and retention policy enforcement (see tasks T033/T036).
