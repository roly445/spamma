# quickstart.md

This feature is small. To implement and test locally:

1. Check out branch `001-bug-sweep` (created by speckit flows).
2. Run the unit tests for `Spamma.Modules.EmailInbox.Tests` and `Spamma.Modules.UserManagement.Tests`.
3. Implement handler checks in `src/modules/Spamma.Modules.EmailInbox/Application/CommandHandlers/Email/`:
   - `DeleteEmailCommandHandler.cs`
   - `ToggleEmailFavoriteCommandHandler.cs`
4. Update `src/Spamma.App/Spamma.App.Client/Components/UserControls/EmailViewer.razor(.cs)` to hide Delete and Favorite buttons when `Email.CampaignId != null`.
5. Add unit tests and API contract test; follow existing test patterns (Moq strict, verification helpers).
6. Run `dotnet build` and `dotnet test`.

