# Spamma Features

## Chaos Address Testing (Experimental)

**Status**: BETA - UI complete, backend implementation in progress

**Purpose**: Test SMTP error handling and bounce management by routing specific email addresses to configured SMTP error responses.

### Overview

Chaos addresses allow you to:

- Define "chaos" email addresses that return specific SMTP error codes when mail is received
- Track how many times each chaos address receives mail
- Enable/disable chaos behavior without deleting configuration
- Test your application's bounce handling and error recovery logic

### Use Cases

1. **SMTP Error Testing**: Verify your application properly handles common SMTP errors (4xx/5xx codes)
2. **Bounce Processing**: Test bounce message parsing and processing workflows
3. **Delivery Fallback**: Validate application fallback logic when email delivery fails
4. **Load Testing**: Simulate various error scenarios during load testing

### How It Works

#### Creating a Chaos Address

1. Navigate to **Chaos Addresses** page (`/chaos-addresses`)
2. Optionally filter by subdomain using the URL parameter: `/chaos-addresses/{subdomainId}`
3. Click **Create Chaos Address**
4. Fill in the form:
   - **Local Part**: The email prefix (e.g., `chaos` creates `chaos@yourdomain.com`)
   - **SMTP Error Code**: Select from predefined error codes (4xx/5xx)
   - **Description**: Optional note about what this address is testing
5. Click **Create** - the address is created in **disabled** state

#### Enabling Chaos Behavior

1. From the Chaos Addresses list, click **Enable** on the desired address
2. Send a test email to the chaos address
3. The SMTP server will return your configured error code to the sender

#### Monitoring

- **Total Received**: Count of emails sent to this address
- **Last Received**: Timestamp of most recent email (UTC format or "Never received")
- **Status Badge**: Shows if address is Enabled or Disabled
- **Immutable Badge**: After first receive, address becomes immutable (no edits/deletes)

#### Immutability

After an address receives its first email:

- ✅ Can still **Enable/Disable** the address
- ❌ Cannot **Edit** the address
- ❌ Cannot **Delete** the address

This prevents accidental changes to configurations that are already in production use and tested.

### Supported SMTP Error Codes

The following error codes are available for testing:

| Code | Category | Meaning | Use Case |
|------|----------|---------|----------|
| 421 | Temporary | Service Not Available | Test temporary failures |
| 450 | Temporary | Mailbox Unavailable | Test temporary unavailability |
| 451 | Temporary | Requested Action Aborted | Test transient errors |
| 452 | Temporary | Insufficient Storage | Test quota/resource issues |
| 500 | Permanent | Syntax Error | Test protocol violations |
| 550 | Permanent | Mailbox Unavailable (Permanent) | Test permanent failures |
| 551 | Permanent | User Not Local | Test invalid recipients |
| 552 | Permanent | Exceeded Storage | Test quota exceeded |
| 553 | Permanent | Mailbox Name Not Allowed | Test invalid addresses |

### Authorization

Chaos addresses can be accessed and managed based on user roles:

| Action | Roles |
|--------|-------|
| **View** | Any authenticated user |
| **Create** | DomainModerator, DomainManagement, SubdomainModerator, SubdomainViewer |
| **Enable/Disable** | DomainModerator, DomainManagement, SubdomainModerator |
| **Delete** | DomainModerator, DomainManagement, SubdomainModerator |

### UI Routes

- **`/chaos-addresses`** - List all chaos addresses across all subdomains
- **`/chaos-addresses/{subdomainId}`** - List chaos addresses for a specific subdomain

### API Endpoints

Backend CQRS implementation:

- **Create**: `POST api/domain-management/create-chaos-address`
- **List**: `GET api/domain-management/chaos-addresses`
- **Enable**: `POST api/domain-management/enable-chaos-address`
- **Disable**: `POST api/domain-management/disable-chaos-address`

### Technical Architecture

#### Client-Side Components

- **`ChaosAddresses.razor`** - Page component with routing and subdomain filtering
- **`ChaosAddressList.razor`** - List display with per-row toggles and authorization
- **`CreateChaosAddress.razor`** - Modal form for creating new chaos addresses

#### Client Queries & Commands

- **`GetChaosAddressesQuery`** - Fetch list of chaos addresses with optional filtering
- **`ChaosAddressSummary`** - DTO for list display
- **`CreateChaosAddressCommand`** - Create a new chaos address
- **`EnableChaosAddressCommand`** - Enable chaos behavior
- **`DisableChaosAddressCommand`** - Disable chaos behavior

#### Backend Processing

Implemented in `Spamma.Modules.EmailInbox`:

When a message is received:

1. Extract recipients from SMTP message
2. Query `GetChaosAddressesBySubdomainQuery` to find matching chaos addresses
3. If match found and **Enabled**:
   - Return configured SMTP error code to sender for that recipient
   - Update chaos address with `TotalReceived++` and `LastReceivedAt`
   - Continue processing other recipients normally (if SMTP server supports per-recipient responses)
4. If no match or **Disabled**: Process normally

### Limitations & Notes

- **Per-recipient responses**: The SMTP server's support for per-recipient responses (RFC 5321) determines whether chaos behavior applies to individual recipients or entire message. See [OBSERVABILITY.md](OBSERVABILITY.md) for SmtpServer library details.
- **Immutability**: Immutable addresses cannot be edited or deleted; if you need to change configuration, create a new address. The immutability prevents accidental changes to tested configurations.
- **Production warning**: Use caution when enabling chaos addresses on production domains; consider using a dedicated test subdomain instead.

### Example Workflow

```text
1. Create chaos address: chaos@testdomain.example.com → Error code 550
2. Disable the address (created disabled)
3. Enable the address when ready to test
4. Application sends test email to chaos@testdomain.example.com
5. SMTP server returns 550 error to application
6. Application processes error, logs bounce, and validates error handling
7. Check "Last Received" and "Total Received" in UI to confirm receipt
```

### Future Enhancements

- [ ] Configurable SMTP response text for each chaos address
- [ ] Chaos address templates for common testing scenarios
- [ ] Automatic replay of stored messages to chaos addresses
- [ ] Email forwarding fallback (chain to another address after first receive)
- [ ] Statistics and analytics dashboard
- [ ] Integration with CI/CD for automated error scenario testing
