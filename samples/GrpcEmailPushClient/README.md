# GrpcEmailPushClient

A minimal console application that demonstrates subscribing to the EmailPush gRPC service (server-streaming) in the Spamma application.

This sample shows how to:
- Generate C# client code from the repository's Protobuf definition
- Connect to a gRPC server using Grpc.Net.Client
- Subscribe to a server-streaming RPC and print incoming notifications

## Prerequisites
- .NET 9 SDK
- Spamma server running locally (optional) or a reachable gRPC endpoint

## Build & Run

Build:

    dotnet build

Run (default server is `https://localhost:7181`):

    dotnet run --project samples/GrpcEmailPushClient/GrpcEmailPushClient.csproj -- --server https://localhost:7181 --api-key <API_KEY> --insecure

Options:

- `--json` - Output notifications in JSON format, useful for piping into other tools.

Example:

    dotnet run --project samples/GrpcEmailPushClient/GrpcEmailPushClient.csproj -- --server https://localhost:7181 --api-key s3cr3tkey --insecure

Load API key from file:

    dotnet run --project samples/GrpcEmailPushClient/GrpcEmailPushClient.csproj -- --server https://localhost:7181 --api-key-file ./secrets/api_key.txt --insecure

Use environment variable:

    # PowerShell
    $env:SPAMMA_API_KEY = "s3cr3tkey"
    dotnet run --project samples/GrpcEmailPushClient/GrpcEmailPushClient.csproj -- --server https://localhost:7181 --insecure

Press Ctrl+C to stop the client.

## Notes

- This client uses header-based API key authentication and requires an API key to start the gRPC streaming connection. The key will be sent in an `X-API-Key` header which matches the server's default ApiKeyAuthenticationOptions header name.
- The client will look for API key using the following priority: `--api-key` option, `--api-key-file` option, and finally the `SPAMMA_API_KEY` environment variable (fallback). Do not store production keys in plaintext.
- Additionally, you can specify `SPAMMA_API_KEY_FILE` environment variable to point to a file that contains the API key.
- The client reads the `email_push.proto` file from the repo. If you change the proto, rebuild the project to regenerate the C# client code.
- This sample is minimal and meant for testing and prototyping only. Production use requires robust authentication and error handling.
- The sample does not use JWT tokens — use API keys for public API authentication instead.

## Getting an API Key (UI)

If you don’t already have an API key, you can create one in the Spamma web UI:

1. Open the Spamma web UI (default: [https://localhost:7181](https://localhost:7181)) and sign in with your user account.
2. Open the Account settings and choose API Keys (or visit: [https://localhost:7181/account/api-keys](https://localhost:7181/account/api-keys)).
3. Click “Create API Key”, enter a descriptive name and copy the generated key. Important: the key is shown once — copy it to a secure place.

You can then use the API key with this sample. Example using curl:

    curl -H "X-API-Key: <YOUR_API_KEY>" \
         https://localhost:7181/api/emails

Use the key with the sample client (CLI `--api-key`, file or env variable):

    dotnet run --project samples/GrpcEmailPushClient/GrpcEmailPushClient.csproj -- --server https://localhost:7181 --api-key <YOUR_API_KEY> --insecure
