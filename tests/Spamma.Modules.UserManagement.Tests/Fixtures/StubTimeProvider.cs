namespace Spamma.Modules.UserManagement.Tests.Fixtures;

/// <summary>
/// A stub TimeProvider for deterministic time-based testing.
/// </summary>
internal class StubTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _fixedUtcNow;

    public StubTimeProvider(DateTime fixedUtcNow)
    {
        _fixedUtcNow = new DateTimeOffset(fixedUtcNow, TimeSpan.Zero);
    }

    public override DateTimeOffset GetUtcNow() => _fixedUtcNow;

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Local;

    public override long GetTimestamp() => _fixedUtcNow.UtcTicks;
}
