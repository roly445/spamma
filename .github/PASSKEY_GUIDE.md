# How to Add a Passkey - User Guide

## Overview
The application now supports passwordless authentication using passkeys (WebAuthn). Users can register security keys on their device and use them to sign in instead of using email magic links.

## For Users - How to Add a Passkey

### Step 1: Navigate to Security Settings
1. Click the **Settings gear icon** ⚙️ in the top-right corner of the application
2. From the dropdown menu, select **Security Settings** (under the "Account" section)

### Step 2: Access the Passkey Manager
You'll see the **Security Keys** section with the **Passkey Manager** component. This shows:
- Your existing passkeys (if any)
- A button to register a new passkey
- Options to revoke passkeys

### Step 3: Register a New Passkey
1. Click **Register New Passkey**
2. Choose your authentication method:
   - **Platform Authenticator** (recommended): Use your device's built-in security (Face ID, Touch ID, Windows Hello, fingerprint)
   - **Security Key**: Use a physical USB security key (FIDO2/U2F compatible)
3. Follow your device's authentication prompt
4. Give your passkey a descriptive name (e.g., "My MacBook", "Office Security Key")

### Step 4: Verify Success
- Your passkey should now appear in the Passkey Manager list
- You'll see details like:
  - Passkey name
  - Created date
  - Last used date
  - Revoke option

## For Developers - How the Passkey System Works

### Architecture

#### Backend (C# .NET)
- **Domain Model**: `Passkey.cs` aggregate manages passkey lifecycle
- **Commands**:
  - `RegisterPasskeyCommand` - Register a new passkey
  - `AuthenticateWithPasskeyCommand` - Use passkey to sign in
  - `RevokePasskeyCommand` - User revokes their own passkey
  - `RevokeUserPasskeyCommand` - Admin revokes any user's passkey
- **Queries**:
  - `GetMyPasskeysQuery` - Get authenticated user's passkeys
  - `GetUserPasskeysQuery` - Get specific user's passkeys (admin)
  - `GetPasskeyDetailsQuery` - Get passkey details

#### Frontend (Blazor WebAssembly)
- **Components**:
  - `PasskeyManager.razor` - Interactive UI component for managing passkeys (client-side)
  - `Security.razor` - Account security settings page with PasskeyManager
  - `PasskeyLogin.razor` - Alternative login page using WebAuthn

- **TypeScript**:
  - `webauthn-utils.ts` - Browser WebAuthn API wrapper with utilities:
    - `isWebAuthnSupported()` - Check browser compatibility
    - `isPlatformAuthenticatorAvailable()` - Check for biometric support
    - `registerCredential()` - Create new passkey
    - `authenticateWithCredential()` - Sign in with passkey

### User Flow

#### Registration Flow
```
User navigates to /account/security
    ↓
Clicks "Register New Passkey"
    ↓
webauthn-utils.ts calls navigator.credentials.create()
    ↓
Browser/OS prompts user for biometric or security key
    ↓
Credential object returned
    ↓
RegisterPasskeyCommand sent to backend
    ↓
Backend validates and stores PublicKeyCredential
    ↓
PasskeyRegistered domain event raised
    ↓
PasskeyManager refreshes to show new passkey
```

#### Authentication Flow
```
User navigates to /login/passkey
    ↓
Clicks "Authenticate with Security Key"
    ↓
webauthn-utils.ts calls navigator.credentials.get()
    ↓
Browser/OS prompts user for biometric or security key
    ↓
Assertion object returned
    ↓
AuthenticateWithPasskeyCommand sent to backend
    ↓
Backend validates signature against stored public key
    ↓
JWT token issued
    ↓
User redirected to app
```

### Navigation

#### For End Users
- **Account Settings**: Settings gear (⚙️) → **Security Settings**
- **Login**: Use the alternative **"Sign in with Security Key"** link on the main login page

#### Routes
- `/account/security` - Security settings page (requires login)
- `/login/passkey` - Alternative login page (not authenticated yet)

### Component Integration

#### AppLayout Menu
The settings dropdown (⚙️ icon) now includes:
```
Account
  └─ Security Settings → /account/security

Administration
  └─ User Management
  └─ Domain Management
  └─ Subdomain Management
```

#### Security.razor Page
- Displays PasskeyManager component
- Shows informational callout about security keys
- Accessible only to authenticated users

#### PasskeyManager Component
- Loads user's passkeys via `GetMyPasskeysQuery`
- Provides UI for:
  - Viewing list of passkeys with metadata
  - Registering new passkeys
  - Revoking existing passkeys
- Handles loading/error states with Tailwind CSS styling

## Extending the System

### Add Passkey to Setup Wizard (Optional)
To add passkey registration during initial setup:

1. Create `setup-passkey.ts` in `Assets/Scripts/`
2. Add passkey step to setup wizard pages (`Admin.razor`)
3. Call `RegisterPasskeyCommand` during setup completion

### Customize Passkey Names
Passkeys currently use auto-generated names. To customize:

1. Add `PasskeyName` property to `RegisterPasskeyCommand`
2. Update `PasskeyManager.razor` UI to include name input
3. Validate name in `RegisterPasskeyCommandValidator`

### Add Passkey Recovery Options
Future enhancements:
- Recovery codes displayed after registration
- Lost passkey procedure
- Admin recovery for locked users
- Notification emails when passkey added/revoked

## Troubleshooting

### Passkey Registration Fails
- Ensure browser supports WebAuthn (Chrome, Firefox, Safari, Edge)
- Check that your device has biometric/security key support
- Verify HTTPS is enabled (required for WebAuthn)

### Passkey Authentication Fails
- Check that sign count validation passes in `Passkey.cs`
- Verify public key stored correctly during registration
- Check browser console for WebAuthn errors

### Component Not Loading
- Verify `Security.razor` page is at `/account/security`
- Check PasskeyManager is in `Components/Passkey/` folder
- Ensure `_Imports.razor` includes `Microsoft.AspNetCore.Components.Sections`

## Files Created/Modified

### New Files
- `src/Spamma.App/Spamma.App.Client/Pages/Account/Security.razor` - Account security page
- `src/Spamma.App/Spamma.App.Client/Components/Passkey/PasskeyManager.razor` - Passkey management UI
- `src/Spamma.App/Spamma.App.Client/Pages/Auth/PasskeyLogin.razor` - WebAuthn login page
- `src/Spamma.App/Spamma.App/Assets/Scripts/webauthn-utils.ts` - WebAuthn browser API wrapper

### Backend Files (Previously Created)
- `src/modules/Spamma.Modules.UserManagement/Domain/PasskeyAggregate/Passkey.cs` - Domain aggregate
- `src/modules/Spamma.Modules.UserManagement/Application/CommandHandlers/` - CQRS handlers
- `src/modules/Spamma.Modules.UserManagement/Application/QueryProcessors/` - Query handlers
- `src/modules/Spamma.Modules.UserManagement/Infrastructure/Repositories/PasskeyRepository.cs` - Repository

### Modified Files
- `src/Spamma.App/Spamma.App.Client/Layout/AppLayout.razor` - Added security settings link
- `src/Spamma.App/Spamma.App.Client/_Imports.razor` - Added required namespaces

## Next Steps
1. Start the application: `dotnet run --project src/Spamma.App/Spamma.App`
2. Log in with your account
3. Navigate to Settings (⚙️) → **Security Settings**
4. Click **Register New Passkey**
5. Follow the prompts to add your first passkey!
