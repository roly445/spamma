namespace Spamma.Modules.DomainManagement.Tests.Fixtures;

/// <summary>
/// Stub TimeProvider for deterministic testing with fixed UTC timestamps.
/// </summary>
internal class StubTimeProvider(DateTime fixedUtcNow) : TimeProvider
{
    private readonly DateTimeOffset _fixedUtcNow = new(fixedUtcNow, TimeSpan.Zero);

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

    public override DateTimeOffset GetUtcNow() => this._fixedUtcNow;
}