using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BluQube.Commands;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.Common.IntegrationEvents.ApiKey;
using Spamma.Modules.UserManagement.Application.Repositories;
using Spamma.Modules.UserManagement.Client.Application.Commands.ApiKeys;
using Spamma.Modules.UserManagement.Client.Application.DTOs;
using Spamma.Modules.UserManagement.Client.Contracts;
using ApiKeyAggregate = Spamma.Modules.UserManagement.Domain.ApiKeys.ApiKey;

namespace Spamma.Modules.UserManagement.Application.CommandHandlers.ApiKeys;

internal class CreateApiKeyCommandHandler(
    IApiKeyRepository apiKeyRepository,
    IIntegrationEventPublisher eventPublisher,
    IHttpContextAccessor httpContextAccessor,
    TimeProvider timeProvider,
    IEnumerable<IValidator<CreateApiKeyCommand>> validators,
    ILogger<CreateApiKeyCommandHandler> logger) : CommandHandler<CreateApiKeyCommand, CreateApiKeyCommandResult>(validators, logger)
{
    protected override async Task<CommandResult<CreateApiKeyCommandResult>> HandleInternal(CreateApiKeyCommand request, CancellationToken cancellationToken)
    {
        // Get current authenticated user ID from claims
        var userIdClaim = httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var currentUserId))
        {
            return CommandResult<CreateApiKeyCommandResult>.Failed(new BluQubeErrorData(UserManagementErrorCodes.InvalidAuthenticationAttempt));
        }

        // Generate API key using PAT pattern
        var apiKeyId = Guid.NewGuid();
        var apiKeyValue = GenerateApiKey();
        var keyHashPrefix = ComputePrefixHash(apiKeyValue);
        var keyHash = BCrypt.Net.BCrypt.HashPassword(apiKeyValue);
        var now = timeProvider.GetUtcNow();

        var result = ApiKeyAggregate.Create(
            apiKeyId,
            currentUserId,
            request.Name,
            keyHashPrefix,
            keyHash,
            now,
            request.WhenExpires);

        if (result.IsFailure)
        {
            return CommandResult<CreateApiKeyCommandResult>.Failed(result.Error);
        }

        var apiKey = result.Value;
        var saveResult = await apiKeyRepository.SaveAsync(apiKey, cancellationToken);
        if (!saveResult.IsSuccess)
        {
            return CommandResult<CreateApiKeyCommandResult>.Failed(new BluQubeErrorData(CommonErrorCodes.SavingChangesFailed));
        }

        // Publish integration event
        await eventPublisher.PublishAsync(
            new ApiKeyCreatedIntegrationEvent(
                apiKey.Id,
                currentUserId,
                apiKey.Name,
                now.UtcDateTime),
            cancellationToken);

        var apiKeySummary = new ApiKeySummary(
            apiKey.Id,
            apiKey.Name,
            now.UtcDateTime,
            apiKey.IsRevoked,
            apiKey.RevokedAt.HasValue ? apiKey.RevokedAt.Value.UtcDateTime : (DateTime?)null,
            request.WhenExpires);

        return CommandResult<CreateApiKeyCommandResult>.Succeeded(
            new CreateApiKeyCommandResult(apiKeySummary, apiKeyValue));
    }

    private static string GenerateApiKey()
    {
        // Generate a random 32-character API key (after "sk-")
        const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        var key = new char[32];
        for (var i = 0; i < key.Length; i++)
        {
            key[i] = chars[random.Next(chars.Length)];
        }

        return "sk-" + new string(key);
    }

    private static string ComputePrefixHash(string apiKey)
    {
        // Extract first 8 characters and compute SHA256 hash for efficient lookup
        var prefix = apiKey.Substring(0, Math.Min(8, apiKey.Length));
        using var sha256 = SHA256.Create();
        var prefixBytes = Encoding.UTF8.GetBytes(prefix);
        var hashBytes = sha256.ComputeHash(prefixBytes);
        return Convert.ToBase64String(hashBytes);
    }
}