using System;
using System.Threading.Tasks;
using Xunit;

namespace Spamma.Modules.EmailInbox.Tests.Infrastructure.Services;

public class SpammaMessageStoreTests
{
    [Fact(Skip = "Integration tests only - cache dependencies")]
    public async Task SaveAsync_Placeholder()
    {
        await Task.CompletedTask;
    }
}
