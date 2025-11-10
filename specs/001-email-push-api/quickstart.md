# Quickstart: Developer First Push API

**Feature**: 001-email-push-api
**Date**: November 10, 2025
**Audience**: Developers integrating with Spamma's email push API

## Overview

The Spamma Push API enables real-time email notifications via gRPC streaming. Developers can subscribe to receive push notifications for emails in their authorized subdomains, with options to filter by all emails, specific addresses, or regex patterns.

## Prerequisites

- Valid Spamma account with viewer access to at least one subdomain
- JWT token for API authentication
- gRPC client library for your programming language
- REST client for integration management

## Step 1: Obtain JWT Token

Authenticate with Spamma to get a JWT token:

```bash
# Example: Login via API to get JWT
curl -X POST https://api.spamma.local/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email": "developer@example.com", "password": "your-password"}'
```

Store the returned JWT token for use in subsequent requests.

## Step 2: Create Push Integration

Create a push integration to define which emails you want to receive notifications for:

```bash
curl -X POST https://api.spamma.local/api/email-push/integrations \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "subdomainId": "123e4567-e89b-12d3-a456-426614174000",
    "name": "My App Integration",
    "filterType": "AllEmails"
  }'
```

**Filter Options**:

- `"filterType": "AllEmails"` - Receive notifications for all emails in the subdomain
- `"filterType": "SingleEmail", "filterValue": "user@domain.com"` - Only emails to specific address
- `"filterType": "Regex", "filterValue": ".*@important-domain\\.com$"` - Emails matching regex pattern

## Step 3: Connect to gRPC Stream

Use your gRPC client to connect to the push service and start receiving notifications:

### C# Example

```csharp
using Grpc.Net.Client;
using Spamma.Modules.EmailInbox.Client.Application.Grpc;

// Create gRPC channel
using var channel = GrpcChannel.ForAddress("https://api.spamma.local");
var client = new EmailPushService.EmailPushServiceClient(channel);

// Create subscription request
var request = new SubscribeRequest
{
    JwtToken = "YOUR_JWT_TOKEN"
};

// Start streaming notifications
using var call = client.SubscribeToEmails(request);
await foreach (var notification in call.ResponseStream.ReadAllAsync())
{
    Console.WriteLine($"New email: {notification.Subject} from {notification.From}");
    // Fetch full content if needed
    await FetchEmailContent(notification.EmailId);
}
```

### Python Example

```python
import grpc
from email_push_pb2 import SubscribeRequest
from email_push_pb2_grpc import EmailPushServiceStub

# Create gRPC channel
channel = grpc.secure_channel('api.spamma.local:443', grpc.ssl_channel_credentials())
stub = EmailPushServiceStub(channel)

# Create subscription request
request = SubscribeRequest(jwt_token='YOUR_JWT_TOKEN')

# Start streaming
responses = stub.SubscribeToEmails(request)
for notification in responses:
    print(f"New email: {notification.subject} from {notification.sender}")
    # Fetch full content if needed
    fetch_email_content(notification.email_id)
```

## Step 4: Fetch Full Email Content

When you receive a notification, fetch the complete email as EML:

```bash
curl -X GET https://api.spamma.local/api/emails/123e4567-e89b-12d3-a456-426614174000/content \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -o email.eml
```

## Step 5: Manage Integrations

### List Integrations

```bash
curl -X GET https://api.spamma.local/api/email-push/integrations \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### Update Integration

```bash
curl -X PUT https://api.spamma.local/api/email-push/integrations/123e4567-e89b-12d3-a456-426614174000 \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "filterType": "Regex",
    "filterValue": ".*@critical-domain\\.com$"
  }'
```

### Delete Integration

```bash
curl -X DELETE https://api.spamma.local/api/email-push/integrations/123e4567-e89b-12d3-a456-426614174000 \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

## Error Handling

### gRPC Status Codes

- `UNAUTHENTICATED` - Invalid or expired JWT token
- `PERMISSION_DENIED` - No access to requested subdomain
- `NOT_FOUND` - Integration or email not found
- `INVALID_ARGUMENT` - Invalid request parameters

### Connection Management

- Implement reconnection logic for network interruptions
- Handle stream timeouts gracefully
- Validate JWT token expiration and refresh as needed

## Best Practices

1. **Connection Management**: Maintain persistent gRPC connections for real-time delivery
2. **Error Handling**: Implement retry logic with exponential backoff
3. **Security**: Never log or expose JWT tokens
4. **Filtering**: Use specific filters to reduce notification volume
5. **Rate Limiting**: Be prepared for high-volume email scenarios

## Troubleshooting

### No Notifications Received

- Verify JWT token is valid and not expired
- Check subdomain viewer permissions
- Confirm integration is active and filter matches emails

### Connection Failures

- Ensure gRPC client is configured for HTTPS
- Check network connectivity to api.spamma.local
- Verify protobuf definitions are up to date

### Permission Errors

- Confirm user has viewer role for the subdomain
- Check JWT token contains correct user claims
- Verify integration belongs to authenticated user