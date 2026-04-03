namespace Company.Application.Helpers;

/// <summary>
/// Helper class for consistent datetime handling across the application.
/// All timestamps use Vietnam timezone (UTC+7).
/// </summary>
public static class DateTimeHelper
{
    private static readonly TimeZoneInfo VietnamTimeZone = 
        TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    /// <summary>
    /// Gets current Vietnam time (UTC+7).
    /// </summary>
    /// <returns>Current datetime in Vietnam timezone</returns>
    public static DateTime GetVietnamTime()
    {
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTimeZone);
    }

    /// <summary>
    /// Converts UTC datetime to Vietnam time.
    /// </summary>
    /// <param name="utcDateTime">UTC datetime to convert</param>
    /// <returns>Datetime in Vietnam timezone</returns>
    public static DateTime ConvertToVietnamTime(DateTime utcDateTime)
    {
        if (utcDateTime.Kind != DateTimeKind.Utc)
        {
            utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        }
        return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, VietnamTimeZone);
    }

    /// <summary>
    /// Converts Vietnam time to UTC.
    /// </summary>
    /// <param name="vietnamDateTime">Vietnam datetime to convert</param>
    /// <returns>Datetime in UTC</returns>
    public static DateTime ConvertToUtc(DateTime vietnamDateTime)
    {
        return TimeZoneInfo.ConvertTimeToUtc(vietnamDateTime, VietnamTimeZone);
    }
}
