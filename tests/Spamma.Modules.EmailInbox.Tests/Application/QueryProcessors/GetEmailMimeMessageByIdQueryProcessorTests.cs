using Xunit;

namespace Spamma.Modules.EmailInbox.Tests.Application.QueryProcessors;

/// <summary>
/// Unit tests for GetEmailMimeMessageByIdQueryProcessor.
/// NOTE: Full integration tests exist in Integration/QueryProcessors/ using Testcontainers + PostgreSQL.
/// These unit tests are placeholders until we have a way to test Marten projections without a database.
/// </summary>
public class GetEmailMimeMessageByIdQueryProcessorTests
{
    [Fact(Skip = "QueryProcessor unit tests require Marten projection mocking - use Integration tests instead")]
    public async Task Handle_WithValidEmailId_ReturnsMimeMessage()
    {
        await Task.CompletedTask;
    }
}