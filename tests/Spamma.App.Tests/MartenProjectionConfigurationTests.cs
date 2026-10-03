using Marten;
using Spamma.Modules.DomainManagement;
using Spamma.Modules.EmailInbox;
using Spamma.Modules.UserManagement;
using Xunit;

namespace Spamma.App.Tests;

public class MartenProjectionConfigurationTests
{
    [Fact]
    public void ModuleProjections_CanBeRegisteredWithoutConflictingReplayConventions()
    {
        var options = new StoreOptions();

        options.ConfigureDomainManagement();
        options.ConfigureEmailInbox();
        options.ConfigureUserManagement();
    }
}
