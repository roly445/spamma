namespace Spamma.Modules.DomainManagement.Tests.Fixtures;

internal class StubTimeProvider(DateTime fixedUtcNow) : TimeProvider
{
    private readonly DateTimeOffset _fixedUtcNow = new(fixedUtcNow, TimeSpan.Zero);

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

    public override DateTimeOffset GetUtcNow() => this._fixedUtcNow;
}