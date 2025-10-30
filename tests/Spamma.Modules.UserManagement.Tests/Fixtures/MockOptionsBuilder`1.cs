using Microsoft.Extensions.Options;

namespace Spamma.Modules.UserManagement.Tests.Fixtures;

public class MockOptionsBuilder<T>
    where T : class, new()
{
    private Guid _primaryUserId = Guid.Empty;

    public MockOptionsBuilder<T> WithPrimaryUserId(Guid userId)
    {
        this._primaryUserId = userId;
        return this;
    }

    public IOptions<T> Build()
    {
        // For Settings, we need to create with init properties
        if (typeof(T) == typeof(Spamma.Modules.Common.Settings))
        {
            var settings = new Spamma.Modules.Common.Settings
            {
                PrimaryUserId = this._primaryUserId,
            };
            return Options.Create(settings as T ?? new T());
        }

        return Options.Create(new T());
    }
}