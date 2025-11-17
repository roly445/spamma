using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Spamma.Modules.UserManagement.Infrastructure.Services.ApiKeys;

namespace Spamma.Modules.EmailInbox.Infrastructure.Services;

public sealed class EmailPushGrpcService(
    PushNotificationManager pushNotificationManager,
    IApiKeyValidationService apiKeyValidationService,
    ILogger<EmailPushGrpcService> logger)
    : global::Spamma.Modules.EmailInbox.Client.Application.Grpc.EmailPushService.EmailPushServiceBase
{
    public override async Task SubscribeToEmails(
        global::Spamma.Modules.EmailInbox.Client.Application.Grpc.SubscribeRequest request,
        IServerStreamWriter<global::Spamma.Modules.EmailInbox.Client.Application.Grpc.EmailNotification> responseStream,
        ServerCallContext context)
    {
        // Validate API key from X-API-Key header
        var apiKey = context.RequestHeaders.FirstOrDefault(h => string.Equals(h.Key, "x-api-key", StringComparison.OrdinalIgnoreCase))?.Value;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            logger.LogWarning("SubscribeToEmails: Missing or empty API key");
            throw new RpcException(new Status(StatusCode.Unauthenticated, "API key is required"));
        }

        var isValid = await apiKeyValidationService.ValidateApiKeyAsync(apiKey, context.CancellationToken);
        if (!isValid)
        {
            logger.LogWarning("SubscribeToEmails: Invalid API key");
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid API key"));
        }

        // Register connection with push notification manager
        var connectionId = Guid.NewGuid().ToString();
        logger.LogInformation("Client subscribed to EmailPush (Connection: {ConnectionId})", connectionId);

        try
        {
            await pushNotificationManager.RegisterConnectionAsync(
                connectionId,
                responseStream,
                Guid.Empty, // User ID would be extracted from API key if needed for filtering
                context);

            // Keep the connection alive until cancellation
            await Task.Delay(Timeout.Infinite, context.CancellationToken);
        }
        catch (OperationCanceledException ex)
        {
            logger.LogInformation(ex, "SubscribeToEmails client disconnected (Connection: {ConnectionId})", connectionId);
        }
        finally
        {
            pushNotificationManager.UnregisterConnection(connectionId);
        }
    }

    public override async Task<global::Spamma.Modules.EmailInbox.Client.Application.Grpc.GetEmailContentResponse> GetEmailContent(
        global::Spamma.Modules.EmailInbox.Client.Application.Grpc.GetEmailContentRequest request,
        ServerCallContext context)
    {
        // Validate API key from X-API-Key header
        var apiKey = context.RequestHeaders.FirstOrDefault(h => string.Equals(h.Key, "x-api-key", StringComparison.OrdinalIgnoreCase))?.Value;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            logger.LogWarning("GetEmailContent: Missing or empty API key");
            throw new RpcException(new Status(StatusCode.Unauthenticated, "API key is required"));
        }

        var isValid = await apiKeyValidationService.ValidateApiKeyAsync(apiKey, context.CancellationToken);
        if (!isValid)
        {
            logger.LogWarning("GetEmailContent: Invalid API key");
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid API key"));
        }

        // Validate email ID format
        if (string.IsNullOrWhiteSpace(request.EmailId))
        {
            logger.LogWarning("GetEmailContent: Invalid email ID");
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Email ID is required"));
        }

        if (!Guid.TryParse(request.EmailId, out var emailId))
        {
            logger.LogWarning("GetEmailContent: Invalid email ID format: {EmailId}", request.EmailId);
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid email ID format"));
        }

        logger.LogInformation("GetEmailContent requested for email: {EmailId}", emailId);

        // Placeholder response - real implementation will retrieve actual email content
        // Future enhancement: Inject IQuerier to call GetEmailMimeMessageByIdQuery
        // and verify user has permission to access this email via the API key
        return new global::Spamma.Modules.EmailInbox.Client.Application.Grpc.GetEmailContentResponse
        {
            MimeMessage = "MIME-Version: 1.0\r\nContent-Type: text/plain; charset=\"utf-8\"\r\n\r\nPlaceholder email content",
        };
    }
}