# Quick Start: API Key Management

**Feature**: API Key Management
**Date**: November 11, 2025
**Status**: ✅ Production Ready

## Overview

This feature replaces JWT authentication with API key authentication for public endpoints. Users can create, manage, and revoke API keys through the web UI. The system includes comprehensive security hardening with audit logging, rate limiting, and key lifecycle management.

## Prerequisites

- Spamma application running
- User account with authentication access
- HTTPS enabled (required for API key authentication)
- Web browser with JavaScript enabled for the management UI

## Creating Your First API Key

1. **Navigate to API Keys**: Log in to Spamma and go to your user settings or API management section.

2. **Create Key**:
   - Click "Create API Key"
   - Enter a descriptive name (e.g., "Production Integration", "Development Testing")
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

**Query Parameter** (alternative, less secure):

```bash
curl "https://api.spamma.local/api/emails?apiKey=sk-abcd1234efgh5678ijkl9012mnop3456"
```

### Available Endpoints

- **Email Inbox API**: Access email data via REST endpoints
- **MIME Message Download**: Download full email content as .eml files
- **Domain Management**: List and manage domains
- **Email Search**: Advanced search capabilities

All public endpoints now require API key authentication.

## Managing API Keys

### Viewing Keys

- Go to API Keys section in the web UI
- See all your keys with metadata:
  - Name (user-defined)
  - Creation date and time
  - Last used timestamp (if applicable)
  - Status (Active/Revoked)
  - Usage statistics

### Revoking Keys

1. Find the key in your list
2. Click "Revoke" button
3. Confirm revocation in the dialog
4. Key becomes inactive immediately

**Warning**: Revoked keys cannot be restored. Create a new key if needed.

### Key Lifecycle

- **Active**: Key can be used for authentication
- **Revoked**: Key is permanently disabled
- **Expired**: Keys can be set to expire automatically (future feature)

## Security Features

### Rate Limiting
- **1000 requests per hour** per API key
- Distributed across all endpoints
- Automatic retry-after headers on limit exceeded

### Audit Logging
- All authentication attempts logged
- Failed attempts tracked for security monitoring
- Usage patterns available for analysis

### Key Security
- **Cryptographically secure** key generation
- **One-time display** - keys never shown again after creation
- **Immediate revocation** capability
- **No key recovery** - enhances security

## API Examples

### List All Emails

```bash
curl -H "X-API-Key: sk-abcd1234efgh5678ijkl9012mnop3456" \
     "https://api.spamma.local/api/emails?page=1&pageSize=10"
```

### Search Emails

```bash
curl -H "X-API-Key: sk-abcd1234efgh5678ijkl9012mnop3456" \
     "https://api.spamma.local/api/emails/search?sender=user@example.com&subject=test"
```

### Download Email as MIME

```bash
curl -H "X-API-Key: sk-abcd1234efgh5678ijkl9012mnop3456" \
     -o email.eml \
     "https://api.spamma.local/api/emails/123e4567-e89b-12d3-a456-426614174000/download"
```

### List Domains

```bash
curl -H "X-API-Key: sk-abcd1234efgh5678ijkl9012mnop3456" \
     "https://api.spamma.local/api/domains"
```

### Get Domain Emails

```bash
curl -H "X-API-Key: sk-abcd1234efgh5678ijkl9012mnop3456" \
     "https://api.spamma.local/api/domains/123e4567-e89b-12d3-a456-426614174000/emails"
```

## Security Best Practices

- **Store securely**: Use environment variables or secure credential stores
- **Rotate regularly**: Create new keys quarterly and revoke old ones
- **Use descriptive names**: Helps identify key usage and revocation needs
- **Monitor usage**: Check application logs for authentication attempts
- **HTTPS only**: API keys are transmitted securely over HTTPS
- **Least privilege**: Create separate keys for different integrations
- **Immediate revocation**: Revoke keys if credentials are compromised

## Troubleshooting

### Authentication Errors

**401 Unauthorized**:
- Check API key is correct and not revoked
- Ensure HTTPS is used (HTTP not supported)
- Verify key belongs to your account
- Check rate limiting hasn't been exceeded

**403 Forbidden**:
- Key may not have required permissions (though none are currently implemented beyond authentication)

**429 Too Many Requests**:
- Rate limiting active; wait before retrying
- Check `Retry-After` header for wait time

### Common Issues

- **Key not working**: Check if it was revoked or expired
- **Cannot create key**: Name may already exist; choose unique name
- **UI not loading**: Ensure JavaScript is enabled for Blazor WebAssembly
- **Rate limited**: Implement exponential backoff in your client
- **Connection errors**: Verify HTTPS and correct API endpoint URL

## Migration from JWT

If you were using JWT tokens:

1. **Create API keys** for your integrations through the web UI
2. **Update client code** to use `X-API-Key` header instead of `Authorization: Bearer`
3. **Test authentication** with new keys on a staging environment
4. **Monitor logs** during transition period
5. JWT authentication has been **completely removed** - update all clients

### Migration Timeline

- **Phase 1**: Dual authentication support (completed)
- **Phase 2**: JWT removal and API key exclusive authentication (✅ completed)
- **Phase 3**: Documentation updates (in progress)

## Advanced Usage

### Programmatic Key Management

While keys are primarily managed through the web UI, the API may support key management endpoints in future versions.

### Integration Examples

**Python with requests:**

```python
import requests

headers = {'X-API-Key': 'sk-abcd1234efgh5678ijkl9012mnop3456'}
response = requests.get('https://api.spamma.local/api/emails', headers=headers)
```

**JavaScript/Node.js:**

```javascript
const axios = require('axios');

const config = {
  headers: {'X-API-Key': 'sk-abcd1234efgh5678ijkl9012mnop3456'}
};

axios.get('https://api.spamma.local/api/emails', config)
  .then(response => console.log(response.data));
```

**Go:**

```go
package main

import (
    "net/http"
    "io/ioutil"
)

func main() {
    client := &http.Client{}
    req, _ := http.NewRequest("GET", "https://api.spamma.local/api/emails", nil)
    req.Header.Set("X-API-Key", "sk-abcd1234efgh5678ijkl9012mnop3456")
    
    resp, _ := client.Do(req)
    body, _ := ioutil.ReadAll(resp.Body)
    println(string(body))
}
```

## Support

For issues:

- Check application logs for detailed error messages
- Verify API key format (starts with `sk-`)
- Ensure HTTPS is used for all requests
- Contact administrators for account-specific issues
- Review rate limiting and usage patterns

## API Reference

For complete OpenAPI/Swagger documentation:
- Visit `/swagger` on your Spamma instance
- Interactive API testing available
- Schema definitions and examples provided