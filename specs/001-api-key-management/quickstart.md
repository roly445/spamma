# Quick Start: API Key Management

**Feature**: API Key Management
**Date**: November 10, 2025

## Overview

This feature replaces JWT authentication with API key authentication for public endpoints. Users can create, manage, and revoke API keys through the web UI.

## Prerequisites

- Spamma application running
- User account with authentication access
- HTTPS enabled (required for API key authentication)

## Creating Your First API Key

1. **Navigate to API Keys**: Log in to Spamma and go to your user settings or API management section.

2. **Create Key**:
   - Click "Create API Key"
   - Enter a descriptive name (e.g., "Production Integration")
   - Click "Generate"

3. **Copy Key Value**: The API key will be displayed **only once**. Copy and store it securely.

   ```text
   Example Key: sk-abcd1234efgh5678ijkl9012mnop3456
   ```

4. **Verify Creation**: The key will appear in your API keys list with status "Active".

## Using API Keys for Authentication

### Authentication Methods

API keys can be provided in two ways:

**Header Authentication** (recommended):

```bash
curl -H "X-API-Key: sk-abcd1234efgh5678ijkl9012mnop3456" \
     https://api.spamma.local/api/emails
```

**Query Parameter** (alternative):

```bash
curl "https://api.spamma.local/api/emails?apiKey=sk-abcd1234efgh5678ijkl9012mnop3456"
```

### Available Endpoints

- **Email Inbox API**: Access email data via gRPC or REST endpoints
- **MIME Message Download**: Download full email content

All public endpoints now require API key authentication.

## Managing API Keys

### Viewing Keys

- Go to API Keys section
- See all your keys with names, creation dates, and status
- Active keys show creation timestamp
- Revoked keys show revocation timestamp

### Revoking Keys

1. Find the key in your list
2. Click "Revoke" button
3. Confirm revocation
4. Key becomes inactive immediately

**Warning**: Revoked keys cannot be restored. Create a new key if needed.

## Security Best Practices

- **Store securely**: Never commit API keys to version control
- **Rotate regularly**: Create new keys and revoke old ones periodically
- **Use descriptive names**: Helps identify key usage and revocation needs
- **Monitor usage**: Check logs for authentication attempts
- **HTTPS only**: API keys are transmitted securely

## Troubleshooting

### Authentication Errors

**401 Unauthorized**:

- Check API key is correct and not revoked
- Ensure HTTPS is used
- Verify key belongs to your account

**403 Forbidden**:

- Key may not have required permissions (though none are currently implemented)

**429 Too Many Requests**:

- Rate limiting active; wait before retrying

### Common Issues

- **Key not working**: Check if it was revoked or expired
- **Cannot create key**: Name may already exist; choose unique name
- **UI not loading**: Ensure JavaScript is enabled for Blazor WebAssembly

## Migration from JWT

If you were using JWT tokens:

1. Create API keys for your integrations
2. Update client code to use `X-API-Key` header
3. Test authentication with new keys
4. JWT authentication will be deprecated in future versions

## Support

For issues:

- Check application logs for detailed error messages
- Verify API key format and validity
- Contact administrators for account-specific issues