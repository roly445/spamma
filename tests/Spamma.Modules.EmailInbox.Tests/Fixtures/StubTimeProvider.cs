namespace Spamma.Modules.EmailInbox.Tests.Fixtures;

/// <summary>
/// A stub TimeProvider for deterministic time-based testing.
/// </summary>
internal class StubTimeProvider(DateTime fixedUtcNow) : TimeProvider
{
    private readonly DateTimeOffset _fixedUtcNow = new(fixedUtcNow, TimeSpan.Zero);

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Local;

    public override DateTimeOffset GetUtcNow() => this._fixedUtcNow;

    public override long GetTimestamp() => this._fixedUtcNow.UtcTicks;
}
