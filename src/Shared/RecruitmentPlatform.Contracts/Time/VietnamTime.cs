namespace RecruitmentPlatform.Contracts.Time;

public static class VietnamTime
{
    private static readonly TimeZoneInfo VietnamTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    public static DateTime Now()
    {
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTimeZone);
    }

    public static DateTime FromUtc(DateTime utcDateTime)
    {
        if (utcDateTime.Kind == DateTimeKind.Local)
        {
            utcDateTime = utcDateTime.ToUniversalTime();
        }
        else if (utcDateTime.Kind == DateTimeKind.Unspecified)
        {
            utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        }

        return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, VietnamTimeZone);
    }
}
