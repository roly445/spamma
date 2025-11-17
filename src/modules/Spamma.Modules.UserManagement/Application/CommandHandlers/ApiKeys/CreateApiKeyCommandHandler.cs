using System.Security.Cryptography;
using System.Text;
using BluQube.Commands;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common;
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
        var userAuthInfo = httpContextAccessor.HttpContext.ToUserAuthInfo();

        if (!userAuthInfo.IsAuthenticated)
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
            userAuthInfo.UserId,
            request.Name,
            keyHashPrefix,
            keyHash,
            now.UtcDateTime,
            request.ExpiresAt);

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
                userAuthInfo.UserId,
                apiKey.Name,
                now.UtcDateTime),
            cancellationToken);

        var apiKeySummary = new ApiKeySummary(
            apiKey.Id,
            apiKey.Name,
            now.UtcDateTime,
            apiKey.IsRevoked,
            apiKey.IsRevoked ? apiKey.RevokedAt : null,
            request.ExpiresAt);

        return CommandResult<CreateApiKeyCommandResult>.Succeeded(
            new CreateApiKeyCommandResult(apiKeySummary, apiKeyValue));
    }

    private static string GenerateApiKey()
    {
        // Generate a cryptographically secure random 32-character API key (after "sk-")
        const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        var key = new char[32];

        using var rng = RandomNumberGenerator.Create();
        var randomBytes = new byte[32];
        rng.GetBytes(randomBytes);

        for (var i = 0; i < key.Length; i++)
        {
            key[i] = chars[randomBytes[i] % chars.Length];
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