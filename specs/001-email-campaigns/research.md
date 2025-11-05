# research.md

Decision: Use asynchronous capture with synchronous receipt audit
Rationale: Keeps SMTP ingestion fast and resilient; receipt audit proves delivery to the system while allowing retries for persistence. Matches spec clarifications.
Alternatives considered:
- Synchronous write before SMTP response — rejected due to potential SMTP timeouts and scalability issues.

Decision: Normalize campaign header values to lower-case
Rationale: Simplifies indexing and matching; avoids duplicate campaigns caused by case variants.
Alternatives considered: preserve original casing and index-case-insensitively — more complex in projections.

Decision: UI will use ApexCharts (with Brush chart for timeline)
Rationale: Lightweight, integrates well with Blazor via JS interop; supports brush/zoom which improves timeline UX. Styling must remain Tailwind-consistent.
Alternatives considered: Chart.js (more heavy), D3 (more work to implement interactions).

Decision: Campaign header name: `x-spamma-camp`
Rationale: User-specified in plan; use lowercase header name and normalize values.

Notes / Research tasks:
- Verify ApexCharts license and integration pattern with Blazor WASM and Tailwind CSS.
- Identify best-practice for Brush chart usage and data aggregation for large volumes (server-side bucketing vs client-side).
- Confirm export approach for CSV/JSON (streaming or in-memory) given default synchronous limit (10k rows).
