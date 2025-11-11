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
- `--api-key` (`-k`) - Optional API key used for header-based authentication (sent in the `X-API-Key` header by default).
- `--jwt` (`-j`) - Optional JWT token to pass to the server (used as a fallback and also included in the request payload if provided).
- `--insecure` (`-i`) - Accept a self-signed certificate (for local development only).

Example:

    dotnet run --project samples/GrpcEmailPushClient/GrpcEmailPushClient.csproj -- --server https://localhost:7181 --api-key s3cr3tkey --insecure

Notes:

- This client prefers header-based API key authentication. When `--api-key` is provided, the key will be sent in an `X-API-Key` header which matches the server's default ApiKeyAuthenticationOptions header name.
- As a convenience, if both `--api-key` and `--jwt` are supplied, `--api-key` will be used in the header and `--jwt` will remain in the request payload. If only `--jwt` is provided, the client will fall back to sending it in an `Authorization: Bearer <token>` header.

Press Ctrl+C to stop the client.

## Notes
- The client reads the `email_push.proto` file from the repo. If you change the proto, rebuild the project to regenerate the C# client code.
- This sample is minimal and meant for testing and prototyping only. Production use requires robust authentication and error handling.

- If `--jwt` is provided, the client will include this token both in the gRPC message payload (`SubscribeRequest.jwt_token`) and as an `Authorization: Bearer <token>` header. This helps the sample interoperate with servers that accept either approach for authentication.
