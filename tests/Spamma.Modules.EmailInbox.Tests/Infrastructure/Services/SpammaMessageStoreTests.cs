using Xunit;

namespace Spamma.Modules.EmailInbox.Tests.Infrastructure.Services;

/// <summary>
/// Integration tests for SpammaMessageStore - tests SMTP message reception flow including:
/// subdomain/chaos address cache lookups, message storage, command execution, and background job queueing.
/// NOTE: These tests are placeholders for future SMTP message store implementation.
/// </summary>
public class SpammaMessageStoreTests
{
    [Fact(Skip = "Not yet implemented: SpammaMessageStore.SaveAsyncWithProvider method does not exist")]
    public async Task SaveAsync_ValidEmailWithActiveSubdomain_QueuesIngestionJobAndReturnsOk()
    {
        await Task.CompletedTask;
    }

    [Fact(Skip = "Not yet implemented: SaveAsyncWithProvider method does not exist")]
    public async Task SaveAsync_NoMatchingSubdomain_ReturnsMailboxNameNotAllowed()
    {
        await Task.CompletedTask;
    }

    [Fact(Skip = "Not yet implemented: SaveAsyncWithProvider method does not exist")]
    public async Task SaveAsync_MessageStorageFailure_ReturnsTransactionFailed()
    {
        await Task.CompletedTask;
    }

    [Fact(Skip = "Not yet implemented: SaveAsyncWithProvider method does not exist")]
    public async Task SaveAsync_FileStorageFailure_ReturnsTransactionFailed()
    {
        await Task.CompletedTask;
    }

    [Fact(Skip = "Not yet implemented: SaveAsyncWithProvider method does not exist")]
    public async Task SaveAsync_WithChaosAddressMatch_ReturnsConfiguredSmtpCode()
    {
        await Task.CompletedTask;
    }

    [Fact(Skip = "Not yet implemented: SaveAsyncWithProvider method does not exist")]
    public async Task SaveAsync_WithDisabledChaosAddress_FallsBackToNormalProcessing()
    {
        await Task.CompletedTask;
    }

    [Fact(Skip = "Not yet implemented: SaveAsyncWithProvider method does not exist")]
    public async Task SaveAsync_WithCampaignHeader_QueuesEmailIngestionJobWithCampaignFlag()
    {
        await Task.CompletedTask;
    }
}
