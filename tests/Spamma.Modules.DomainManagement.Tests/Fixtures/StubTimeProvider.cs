namespace Spamma.Modules.DomainManagement.Tests.Fixtures;

/// <summary>
/// Stub TimeProvider for deterministic testing with fixed UTC timestamps.
/// </summary>
internal class StubTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _fixedUtcNow;

    public StubTimeProvider(DateTime fixedUtcNow)
    {
        _fixedUtcNow = new DateTimeOffset(fixedUtcNow, TimeSpan.Zero);
    }

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

    public override DateTimeOffset GetUtcNow() => _fixedUtcNow;
}
