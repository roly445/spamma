# Work Routing

How to decide who handles what.

## Routing Table

| Work Type | Route To | Examples |
|-----------|----------|----------|
| Architecture, scope, trade-offs, code review | Master Chief | Module design, CQRS patterns, API shape, PR reviews |
| Backend / .NET | Cortana | Command/query handlers, Marten projections, integration events, FluentValidation |
| Frontend / Blazor | Johnson | Blazor WASM components, Tailwind CSS, TypeScript assets, setup wizards |
| Testing / QA | Arbiter | xUnit tests, domain aggregate tests, handler mocks, edge cases |
| DevOps / Infrastructure | Guilty Spark | Docker Compose, CI/CD, PostgreSQL, Redis, environment config |
| Security / Auth | Halsey | WebAuthn / passkeys, magic links, cookie auth, authorization policies |
| Email / SMTP | Foehammer | SmtpServer, MimeKit, SpammaMessageStore, IMessageStoreProvider |
| Docs / DevRel | Virgil | README, API docs, architecture notes, changelog |
| Code review | Master Chief | Review PRs, check quality, suggest improvements |
| Testing | Arbiter | Write tests, find edge cases, verify fixes |
| Scope & priorities | Master Chief | What to build next, trade-offs, decisions |
| Session logging | Scribe | Automatic — never needs routing |

## Issue Routing

| Label | Action | Who |
|-------|--------|-----|
| `squad` | Triage: analyze issue, assign `squad:{member}` label | Master Chief |
| `squad:master-chief` | Architecture, design, review work | Master Chief |
| `squad:cortana` | Backend .NET work | Cortana |
| `squad:johnson` | Frontend Blazor/UI work | Johnson |
| `squad:arbiter` | Testing and QA | Arbiter |
| `squad:guilty-spark` | DevOps / infra work | Guilty Spark |
| `squad:halsey` | Security / auth work | Halsey |
| `squad:foehammer` | Email / SMTP work | Foehammer |
| `squad:virgil` | Docs and DevRel | Virgil |

### How Issue Assignment Works

1. When a GitHub issue gets the `squad` label, the **Lead** triages it — analyzing content, assigning the right `squad:{member}` label, and commenting with triage notes.
2. When a `squad:{member}` label is applied, that member picks up the issue in their next session.
3. Members can reassign by removing their label and adding another member's label.
4. The `squad` label is the "inbox" — untriaged issues waiting for Lead review.

## Rules

1. **Eager by default** — spawn all agents who could usefully start work, including anticipatory downstream work.
2. **Scribe always runs** after substantial work, always as `mode: "background"`. Never blocks.
3. **Quick facts → coordinator answers directly.** Don't spawn an agent for "what port does the server run on?"
4. **When two agents could handle it**, pick the one whose domain is the primary concern.
5. **"Team, ..." → fan-out.** Spawn all relevant agents in parallel as `mode: "background"`.
6. **Anticipate downstream work.** If a feature is being built, spawn the tester to write test cases from requirements simultaneously.
7. **Issue-labeled work** — when a `squad:{member}` label is applied to an issue, route to that member. The Lead handles all `squad` (base label) triage.
