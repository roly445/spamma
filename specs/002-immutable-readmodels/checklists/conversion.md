# Readmodel Conversion Checklist

**Feature**: `002-immutable-readmodels` | **Date**: November 17, 2025

---

## Conversion Progress

### UserManagement Module

#### PasskeyProjection
- [ ] Convert all properties to private setters
- [ ] Run `dotnet build Spamma.sln` - verify zero warnings
- [ ] Projection tests pass: `dotnet test tests/Spamma.Modules.UserManagement.Tests/Infrastructure/Projections/`
- [ ] Commit: `git commit -m "refactor(UserManagement): Make PasskeyProjection properties immutable"`

#### ApiKeyProjection
- [ ] Convert all properties to private setters
- [ ] Run `dotnet build Spamma.sln` - verify zero warnings
- [ ] Projection tests pass
- [ ] Commit: `git commit -m "refactor(UserManagement): Make ApiKeyProjection properties immutable"`

#### UserLookup
- [ ] Convert scalar properties to private setters
- [ ] Convert collections to getter-only: `{ get; } = new()`
- [ ] Run `dotnet build Spamma.sln` - verify zero warnings
- [ ] Projection tests pass
- [ ] Commit: `git commit -m "refactor(UserManagement): Make UserLookup properties immutable"`

---

### DomainManagement Module

#### SubdomainLookup
- [ ] Convert all properties to private setters
- [ ] Run `dotnet build Spamma.sln` - verify zero warnings
- [ ] Projection tests pass: `dotnet test tests/Spamma.Modules.DomainManagement.Tests/Infrastructure/Projections/`
- [ ] Commit: `git commit -m "refactor(DomainManagement): Make SubdomainLookup properties immutable"`

#### ChaosAddressLookup
- [ ] Convert all properties to private setters
- [ ] Run `dotnet build Spamma.sln` - verify zero warnings
- [ ] Projection tests pass
- [ ] Commit: `git commit -m "refactor(DomainManagement): Make ChaosAddressLookup properties immutable"`

#### DomainLookup
- [ ] Convert scalar properties to private setters
- [ ] Convert collections to getter-only: `{ get; } = new()`
- [ ] Run `dotnet build Spamma.sln` - verify zero warnings
- [ ] Projection tests pass
- [ ] Commit: `git commit -m "refactor(DomainManagement): Make DomainLookup properties immutable"`

---

### EmailInbox Module

#### EmailLookup
- [ ] Convert all properties to private setters
- [ ] Run `dotnet build Spamma.sln` - verify zero warnings
- [ ] Projection tests pass: `dotnet test tests/Spamma.Modules.EmailInbox.Tests/Infrastructure/Projections/`
- [ ] Commit: `git commit -m "refactor(EmailInbox): Make EmailLookup properties immutable"`

#### CampaignSummary
- [ ] Convert all properties to private setters
- [ ] Run `dotnet build Spamma.sln` - verify zero warnings
- [ ] Projection tests pass
- [ ] Commit: `git commit -m "refactor(EmailInbox): Make CampaignSummary properties immutable"`

---

## Build Validation

After all readmodels converted:

- [ ] **Full Build**: `dotnet build Spamma.sln` - Must have **zero warnings**
- [ ] **StyleCop Check**: Verify no SA1206 violations (public setters should be caught)
- [ ] **All Projection Tests**: `dotnet test tests/Spamma.Modules.*/Infrastructure/Projections/` - All pass

---

## Backward Compatibility Test

- [ ] Create backward compatibility test file
- [ ] Test that existing Marten documents deserialize into immutable readmodels
- [ ] Verify no data migration needed
- [ ] Commit: `git commit -m "test: Verify backward compatibility with existing Marten documents"`

---

## Final Verification

- [ ] All 8 readmodels converted
- [ ] All projection tests passing (no regressions)
- [ ] Zero compiler warnings
- [ ] Backward compatibility validated
- [ ] Ready for code review PR

---

## Reference

See `quickstart.md` for conversion patterns and examples.
