namespace Journal.Tests.TestSupport;

public sealed class FixedTimeProvider : TimeProvider
{
    private DateTime _now;

    public FixedTimeProvider(DateTime now)
    {
        _now = now;
    }

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

    public void Advance(TimeSpan amount)
    {
        _now += amount;
    }

    public override DateTimeOffset GetUtcNow()
    {
        return new DateTimeOffset(DateTime.SpecifyKind(_now, DateTimeKind.Utc));
    }
}
