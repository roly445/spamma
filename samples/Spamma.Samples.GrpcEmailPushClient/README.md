# GrpcEmailPushClient

A minimal console application that demonstrates subscribing to real-time email notifications and fetching full email content from the Spamma EmailPush gRPC service.

This sample shows how to:
- Generate C# client code from the repository's Protobuf definition
- Connect to a gRPC server using Grpc.Net.Client
- Subscribe to a server-streaming RPC for real-time email notifications
- Fetch full email content via unary RPC
- Parse MIME messages and extract email bodies
- Use header-based API key authentication with gRPC

## Prerequisites
- .NET 9 SDK
- Spamma server running locally (optional) or a reachable gRPC endpoint

## Build & Run

Build:

    dotnet build

Run (default server is `https://localhost:7181`):

    dotnet run --project samples/GrpcEmailPushClient/GrpcEmailPushClient.csproj -- --server https://localhost:7181 --api-key <API_KEY> --insecure

Options:

- `--server <url>` - gRPC server URL (default: `https://localhost:7181`)
- `--api-key <key>` - API key for authentication
- `--api-key-file <path>` - Path to file containing API key
- `--insecure` - Accept self-signed certificates (for local development with HTTPS)
- `--json` - Output notifications in JSON format
- `--verbose` - Show detailed debug information

Example with all options:

    dotnet run --project samples/GrpcEmailPushClient/GrpcEmailPushClient.csproj -- \
      --server https://localhost:7181 \
      --api-key s3cr3tkey \
      --insecure \
      --json \
      --verbose

Load API key from file:

    dotnet run --project samples/GrpcEmailPushClient/GrpcEmailPushClient.csproj -- --server https://localhost:7181 --api-key-file ./secrets/api_key.txt --insecure

Use environment variable:

    # PowerShell
    $env:SPAMMA_API_KEY = "s3cr3tkey"
    dotnet run --project samples/GrpcEmailPushClient/GrpcEmailPushClient.csproj -- --server https://localhost:7181 --insecure

Press Ctrl+C to stop the client and disconnect from the stream.

## What This Sample Does

1. **Connects to Spamma**: Opens a gRPC connection using the provided API key for authentication
2. **Subscribes to Email Notifications**: Opens a server-streaming RPC connection to receive real-time email notifications as they arrive
3. **Downloads Email Content**: For each notification, fetches the full email content via a unary RPC call
4. **Parses MIME**: Extracts the email body (HTML preferred, falls back to text) from the MIME message
5. **Displays Results**: Shows the email metadata and body content in the console

## Output Example

```
[Email] Id=3fa85d8c-12ab-4def-8901-234567890abc, To=user@example.com, From=sender@example.com, Subject="Test Email", ReceivedAt=2025-11-15 14:30:45
--- Email Body (HTML) ---
<html>
  <body>
    <p>This is a test email with HTML content</p>
  </body>
</html>
--- End of Body ---
```

With `--json` flag:

```json
{
  "id": "3fa85d8c-12ab-4def-8901-234567890abc",
  "to": "user@example.com",
  "from": "sender@example.com",
  "subject": "Test Email",
  "receivedAt": "2025-11-15 14:30:45"
}
```

## How It Works

The client opens a long-lived streaming connection to the `EmailPushService.SubscribeToEmails()` RPC. When Spamma receives an email for a configured domain, it sends a notification to all connected subscribers. The client receives each notification and immediately calls `EmailPushService.GetEmailContent()` to fetch the full email. It then parses the MIME message to extract the HTML or text body for display.

The connection remains open indefinitely until you press Ctrl+C or the server closes the stream.

## Notes

- This client uses header-based API key authentication and requires an API key to start the gRPC streaming connection. The key will be sent in an `X-API-Key` header.

- The client will look for an API key using the following priority:
  1. `--api-key <key>` command-line option
  2. `--api-key-file <path>` command-line option (reads from file)
  3. `SPAMMA_API_KEY` environment variable
  4. `SPAMMA_API_KEY_FILE` environment variable (reads from file)

  Never store production keys in plaintext or commit to version control.

- Note: HTTP headers are case-insensitive, so `X-API-Key`, `x-api-key`, and `X-Api-Key` are equivalent.
- The client reads the `email_push.proto` file from the repo. If you change the proto, rebuild the project to regenerate the C# client code.
- This sample is minimal and meant for testing and prototyping only. Production use requires robust authentication and error handling.
- The sample does not use JWT tokens — it uses API keys for header-based authentication with gRPC.

## Getting an API Key

Create an API key in the Spamma web UI:

1. Open the Spamma web UI (default: [https://localhost:7181](https://localhost:7181)) and sign in
2. Navigate to Account → API Keys (or visit [https://localhost:7181/account/api-keys](https://localhost:7181/account/api-keys))
3. Click "Create API Key", enter a descriptive name, and copy the generated key
4. **Important**: The key is displayed once only — save it securely

Use the key with the sample client:

    dotnet run --project samples/GrpcEmailPushClient/GrpcEmailPushClient.csproj -- \
      --server https://localhost:7181 \
      --api-key <YOUR_API_KEY> \
      --insecure
