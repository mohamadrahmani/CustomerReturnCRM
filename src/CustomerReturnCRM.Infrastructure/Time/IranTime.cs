namespace CustomerReturnCRM.Infrastructure.Time;

public static class IranTime
{
    private static readonly TimeZoneInfo Zone = ResolveZone();

    public static DateTime UtcNow(TimeProvider provider) =>
        provider.GetUtcNow().UtcDateTime;

    public static DateTime Now(TimeProvider provider) =>
        TimeZoneInfo.ConvertTime(provider.GetUtcNow(), Zone).DateTime;

    public static DateTime ToIran(DateTime utcDateTime) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc), Zone);

    public static DateTime StartOfTodayUtc(TimeProvider provider)
    {
        var localToday = Now(provider).Date;
        return TimeZoneInfo.ConvertTimeToUtc(localToday, Zone);
    }

    private static TimeZoneInfo ResolveZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Tehran"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Iran Standard Time"); }
    }
}
