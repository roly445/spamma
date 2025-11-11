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

- `--server` (`-s`) - gRPC server URL (defaults to https://localhost:7181).
- `--api-key` (`-k`) - API key used for header-based authentication (sent in the `X-API-Key` header by default). This client requires an API key to authenticate with the Push API.
- `--insecure` (`-i`) - Accept a self-signed certificate (for local development only).

Example:

    dotnet run --project samples/GrpcEmailPushClient/GrpcEmailPushClient.csproj -- --server https://localhost:7181 --api-key s3cr3tkey --insecure

Press Ctrl+C to stop the client.

## Notes

- This client uses header-based API key authentication and requires an API key to start the gRPC streaming connection. The key will be sent in an `X-API-Key` header which matches the server's default ApiKeyAuthenticationOptions header name.
- The client reads the `email_push.proto` file from the repo. If you change the proto, rebuild the project to regenerate the C# client code.
- This sample is minimal and meant for testing and prototyping only. Production use requires robust authentication and error handling.
- The sample does not use JWT tokens — use API keys for public API authentication instead.
