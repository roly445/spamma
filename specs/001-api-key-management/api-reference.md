# API Reference: API Key Authentication

**Version**: 1.0.0
**Date**: November 11, 2025
**Authentication**: API Key (JWT removed)

## Overview

Spamma provides RESTful APIs for programmatic access to email data, domains, and administrative functions. All public endpoints require API key authentication.

## Authentication

### API Key Format

API keys follow the format: `sk-{32-character-hex-string}`

Example: `sk-abcd1234efgh5678ijkl9012mnop3456`

### Authentication Methods

#### Header Authentication (Recommended)

```http
X-API-Key: sk-abcd1234efgh5678ijkl9012mnop3456
```

#### Query Parameter Authentication (Alternative)

```http
GET /api/emails?apiKey=sk-abcd1234efgh5678ijkl9012mnop3456
```

**Security Note**: Query parameter authentication is less secure as keys may appear in logs, browser history, or server access logs.

### Security Features

- **Rate Limiting**: 1000 requests/hour per API key
- **Audit Logging**: All authentication attempts logged
- **HTTPS Required**: All requests must use HTTPS
- **Key Validation**: Real-time validation against database
- **Caching**: Optimized validation with distributed caching

## API Endpoints

### Base URL

```
https://your-spamma-instance.com
```

### Email Endpoints

#### List Emails

**Endpoint**: `GET /api/emails`

**Description**: Retrieve paginated list of emails accessible to the API key owner.

**Parameters**:

- `page` (optional): Page number (default: 1)
- `pageSize` (optional): Items per page (default: 50, max: 100)

**Response**: 200 OK

```json
{
  "items": [
    {
      "id": "123e4567-e89b-12d3-a456-426614174000",
      "subject": "Test Email",
      "sender": "sender@example.com",
      "recipient": "recipient@spamma.io",
      "receivedAt": "2025-11-11T10:30:00Z",
      "size": 1024
    }
  ],
  "totalCount": 150,
  "pageNumber": 1,
  "pageSize": 50,
  "totalPages": 3
}
```

#### Get Email Details

**Endpoint**: `GET /api/emails/{emailId}`

**Description**: Retrieve detailed information about a specific email.

**Parameters**:

- `emailId`: UUID of the email

**Response**: 200 OK

```json
{
  "id": "123e4567-e89b-12d3-a456-426614174000",
  "subject": "Test Email",
  "sender": "sender@example.com",
  "recipients": ["recipient@spamma.io"],
  "cc": [],
  "bcc": [],
  "receivedAt": "2025-11-11T10:30:00Z",
  "size": 1024,
  "hasAttachments": false,
  "domainId": "456e7890-e89b-12d3-a456-426614174001"
}
```

#### Search Emails

**Endpoint**: `GET /api/emails/search`

**Description**: Search emails with filters.

**Parameters**:

- `sender` (optional): Filter by sender email
- `subject` (optional): Filter by subject (partial match)
- `domainId` (optional): Filter by domain ID
- `receivedAfter` (optional): ISO 8601 date filter
- `receivedBefore` (optional): ISO 8601 date filter
- `page` (optional): Page number (default: 1)
- `pageSize` (optional): Items per page (default: 50, max: 100)

**Response**: 200 OK (same format as List Emails)

#### Download Email MIME

**Endpoint**: `GET /api/emails/{emailId}/download`

**Description**: Download the full MIME message as .eml file.

**Parameters**:

- `emailId`: UUID of the email

**Response**: 200 OK with `application/octet-stream` content

**Headers**:

```http
Content-Disposition: attachment; filename="email.eml"
Content-Type: application/octet-stream
```

#### Delete Email

**Endpoint**: `DELETE /api/emails/{emailId}`

**Description**: Delete an email (soft delete - marked as deleted).

**Parameters**:

- `emailId`: UUID of the email

**Response**: 204 No Content

### Domain Endpoints

#### List Domains

**Endpoint**: `GET /api/domains`

**Description**: Retrieve domains accessible to the API key owner.

**Response**: 200 OK

```json
{
  "items": [
    {
      "id": "456e7890-e89b-12d3-a456-426614174001",
      "name": "spamma.io",
      "status": "Active",
      "createdAt": "2025-11-01T09:00:00Z"
    }
  ],
  "totalCount": 5
}
```

#### Get Domain Emails

**Endpoint**: `GET /api/domains/{domainId}/emails`

**Description**: Retrieve emails for a specific domain.

**Parameters**:

- `domainId`: UUID of the domain
- `page` (optional): Page number (default: 1)
- `pageSize` (optional): Items per page (default: 50, max: 100)

**Response**: 200 OK (same format as List Emails)

## Error Responses

### Authentication Errors

#### 401 Unauthorized

```json
{
  "type": "AuthenticationError",
  "message": "Invalid or missing API key",
  "code": "INVALID_API_KEY"
}
```

#### 429 Too Many Requests

```json
{
  "type": "RateLimitError",
  "message": "Rate limit exceeded",
  "code": "RATE_LIMIT_EXCEEDED",
  "retryAfter": 3600
}
```

### General Errors

#### 400 Bad Request

```json
{
  "type": "ValidationError",
  "message": "Invalid request parameters",
  "errors": [
    {
      "field": "pageSize",
      "message": "Must be between 1 and 100"
    }
  ]
}
```

#### 403 Forbidden

```json
{
  "type": "AuthorizationError",
  "message": "Access denied to requested resource",
  "code": "ACCESS_DENIED"
}
```

#### 404 Not Found

```json
{
  "type": "NotFoundError",
  "message": "Resource not found",
  "code": "RESOURCE_NOT_FOUND"
}
```

#### 500 Internal Server Error

```json
{
  "type": "ServerError",
  "message": "An unexpected error occurred",
  "code": "INTERNAL_ERROR"
}
```

## Rate Limiting

- **Limit**: 1000 requests per hour per API key
- **Reset**: Hourly at the start of each hour
- **Headers**: Rate limit information included in responses

```http
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 950
X-RateLimit-Reset: 2025-11-11T11:00:00Z
```

## Data Types

### Email

```typescript
interface Email {
  id: string;              // UUID
  subject: string;
  sender: string;          // Email address
  recipients: string[];    // Email addresses
  cc?: string[];          // Email addresses
  bcc?: string[];         // Email addresses
  receivedAt: string;     // ISO 8601 timestamp
  size: number;           // Bytes
  hasAttachments: boolean;
  domainId: string;       // UUID
}
```

### Domain

```typescript
interface Domain {
  id: string;             // UUID
  name: string;           // Domain name
  status: 'Active' | 'Suspended';
  createdAt: string;      // ISO 8601 timestamp
}
```

### Paginated Response

```typescript
interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}
```

## SDKs and Libraries

### Official SDKs

- **None available yet** - Use standard HTTP clients

### Community Libraries

**Python**:

```python
import requests

class SpammaClient:
    def __init__(self, api_key, base_url="https://api.spamma.local"):
        self.api_key = api_key
        self.base_url = base_url
        self.session = requests.Session()
        self.session.headers.update({'X-API-Key': api_key})

    def get_emails(self, page=1, page_size=50):
        response = self.session.get(f"{self.base_url}/api/emails",
                                  params={'page': page, 'pageSize': page_size})
        return response.json()
```

## Best Practices

### Security

1. **Use HTTPS**: Always use HTTPS for all API requests
2. **Secure Key Storage**: Never commit API keys to version control
3. **Key Rotation**: Rotate keys regularly (quarterly recommended)
4. **Monitor Usage**: Check logs for unusual activity
5. **Least Privilege**: Use separate keys for different integrations

### Performance

1. **Pagination**: Use appropriate page sizes (50-100 items)
2. **Caching**: Cache responses when appropriate
3. **Rate Limits**: Implement exponential backoff for retries
4. **Connection Reuse**: Reuse HTTP connections when possible

### Error Handling

1. **Check Status Codes**: Handle 4xx and 5xx errors appropriately
2. **Rate Limit Handling**: Respect Retry-After headers
3. **Idempotency**: Design requests to be safely retryable
4. **Logging**: Log API errors for debugging

## Changelog

### Version 1.0.0 (November 11, 2025)

- Initial release with API key authentication
- JWT authentication completely removed
- Rate limiting and audit logging implemented
- Full REST API for emails and domains

## Support

- **Documentation**: `/swagger` endpoint on your Spamma instance
- **Issues**: Check application logs for detailed error information
- **Rate Limits**: Monitor `X-RateLimit-*` headers
- **Contact**: Administrator support for account-specific issues
