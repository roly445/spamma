# research.md

Feature: Bug sweep — campaign-protection & UI fixes

## Unknowns & Decisions

- Decision: Protect when `CampaignId` is non-null (recorded in spec). Reason: simple, O(1) check; avoids extra lookups.
- Decision: Use BluQube error semantics (`CommandResult` with `BluQubeErrorData`, error code `EmailIsPartOfCampaign`). Reason: aligns with project conventions and existing handler patterns.
- Decision: No admin/force override; cascade-delete is synchronous and safe because campaigns are 1:1 with emails. Owner opted out of audit logs.

## Research Tasks

- Verify where `CampaignId` is modeled on the `Email` aggregate and ensure nullable GUID exists.
- Identify handler files for DeleteEmail and ToggleEmailFavorite commands (located under EmailInbox Application/CommandHandlers/Email).
- Identify existing error codes and EmailInboxErrorCodes location for adding/using `EmailIsPartOfCampaign` if not present.
- UI: Locate `EmailViewer` component and client call sites to hide controls when `Email.CampaignId != null`.

## Findings

- `Email` aggregate includes checks for deleted/favorited states; no CampaignId guard present (handlers must enforce).
- `EmailInboxErrorCodes.cs` exists under `src/modules/Spamma.Modules.EmailInbox.Client/Contracts/` and is the correct place for a new error code.
- `DeleteEmailCommandHandler.cs` and `ToggleEmailFavoriteCommandHandler.cs` exist and are the right edit points.
- `EmailViewer.razor(.cs)` calls Commander to send `DeleteEmailCommand` and `ToggleEmailFavoriteCommand` on user actions; hiding the buttons requires adding conditional markup and possibly updating tests.

## Decision Summary

- Implement handler checks and return `CommandResult.Failed(new BluQubeErrorData(EmailInboxErrorCodes.EmailIsPartOfCampaign))` when `Email.CampaignId != null`.
- Add `EmailIsPartOfCampaign` to `EmailInboxErrorCodes` if missing.
- Update `EmailViewer` to hide Delete and Favorite controls when `Email.CampaignId != null`.
- Add unit tests for handlers and an API-level contract test to assert `BluQubeErrorData` error code.

