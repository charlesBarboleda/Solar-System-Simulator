using System;
using System.Globalization;
using UnityEngine;

public static class HorizonsAPIParameters
{
    // Julian Day & Modified constants
    const double JD_UNIX_EPOCH = 2440587.5;
    const double JD_MINUS_MJD = 2400000.5;
    const string DATETIME_FORMAT = "yyyy-MM-dd HH:mm:ss.fff 'UTC'";

    public static bool IsValidYear(string year, out int y)
    {
        if (!int.TryParse(year, NumberStyles.Integer, CultureInfo.InvariantCulture, out y) || y < 1 || y > 9999)
        {
            Debug.LogWarning($"Invalid Year input '{year}'");
            return false;
        }

        return true;
    }

    public static bool IsValidMonth(string month, out int m)
    {
        if (!int.TryParse(month, NumberStyles.Integer, CultureInfo.InvariantCulture, out m) || m < 1 || m > 12)
        {
            Debug.LogWarning($"Invalid Month input '{month}'");
            return false;
        }

        return true;
    }

    public static bool IsValidDay(string day, int month, int year, out int d)
    {
        if (!int.TryParse(day, NumberStyles.Integer, CultureInfo.InvariantCulture, out d))
        {
            Debug.LogWarning($"Invalid Day input '{day}'");
            return false;
        }

        int maxDay = DateTime.DaysInMonth(year, month);
        if (d < 1 || d > maxDay)
        {
            Debug.LogWarning($"Invalid Day input '{day}' for {year:D4}-{month:D2} (max {maxDay})");
            return false;
        }

        return true;
    }

    public static bool IsValidHour(string hour, out int h)
    {
        if (!int.TryParse(hour, NumberStyles.Integer, CultureInfo.InvariantCulture, out h) || h < 0 || h > 23)
        {
            Debug.LogWarning($"Invalid Hour input '{hour}'");
            return false;
        }

        return true;
    }

    public static bool IsValidMinute(string minute, out int min)
    {
        if (!int.TryParse(minute, NumberStyles.Integer, CultureInfo.InvariantCulture, out min) || min < 0 || min > 59)
        {
            Debug.LogWarning($"Invalid Minute input '{minute}'");
            return false;
        }

        return true;
    }

    public static bool IsValidSecond(string second, out double sec)
    {
        if (!double.TryParse(second, NumberStyles.Float, CultureInfo.InvariantCulture, out sec) || sec < 0.0 || sec >= 60.0)
        {
            Debug.LogWarning($"Invalid Second input '{second}'");
            return false;
        }

        return true;
    }

    static string StripUtcSuffix(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return s.EndsWith(" UTC", StringComparison.Ordinal) ? s[..^4] : s;
    }

    public static bool IsValidStartStopTime(
       DateTimeOffset startUtc,
       DateTimeOffset stopUtc,
       bool allowEqual = false)
    {

        int cmp = DateTimeOffset.Compare(stopUtc, startUtc);

        if (allowEqual)
        {
            if (cmp < 0)
            {
                UIMessage.Instance.NewFadingMessage(MessageType.Error, $"STOP_TIME must be greater than or equal to START_TIME", 20f);
                return false;
            }
            return true;
        }

        if (cmp <= 0)
        {
            UIMessage.Instance.NewFadingMessage(MessageType.Error, $"STOP_TIME must be strictly greater than START_TIME", 20f);
            return false;
        }

        return true;
    }

    public static bool TryBuildUTCTime(DateTimeOffset unixEpoch, long ticks, out string UTCTime, bool stripUTC = false)
    {
        UTCTime = string.Empty;

        try
        {
            DateTimeOffset dto = unixEpoch + TimeSpan.FromTicks(ticks);
            UTCTime = dto.UtcDateTime.ToString(DATETIME_FORMAT, CultureInfo.InvariantCulture);
            if (stripUTC) UTCTime = StripUtcSuffix(UTCTime);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            Debug.LogWarning($"Julian Day date out of range: unixEpoch={unixEpoch}, ticks={ticks}");
            return false;
        }
    }

    public static bool TryBuildUTCTime(int year, int month, int day, int hour, int minute, double second, out string UTCTime, bool stripUTC = false)
    {
        UTCTime = string.Empty;

        try
        {
            var dto = new DateTimeOffset(year, month, day, hour, minute, 0, TimeSpan.Zero).AddSeconds(second);
            UTCTime = dto.UtcDateTime.ToString(DATETIME_FORMAT, CultureInfo.InvariantCulture);
            if (stripUTC) UTCTime = StripUtcSuffix(UTCTime);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            Debug.LogWarning($"Calendar date out of range: y={year}, m={month}, d={day}, h={hour}, min={minute}, sec={second}");
            return false;
        }
    }

    public static bool IsValidNAIFID(string input, out int NAIFID)
    {
        if (!int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out NAIFID)) return false;

        return true;
    }

    public static string EncodeQuoted(string inner) => "%27" + Uri.EscapeDataString(inner ?? string.Empty) + "%27";


    public enum CalendarParseFailReason
    {
        InvalidYear,
        InvalidMonth,
        InvalidDay,
        InvalidHour,
        InvalidMinute,
        InvalidSecond,
        BuildUtcFailed
    }
    public enum CalendarParseSuccessReason
    {
        Year,
        Month,
        Day,
        Hour,
        Minute,
        Second,
        BuildUtc
    }
    public static bool TryParseCalendarDay(
    string year,
    out string dateTime,
    string month = "1",
    string day = "1",
    string hour = "0",
    string minute = "0",
    string second = "0",
    Action<CalendarParseFailReason> onFail = null,
    Action<CalendarParseSuccessReason> onSuccess = null,
    bool stripUTC = false)
    {
        dateTime = default;
        bool didFail = false;

        // Year (1-9999)
        if (!IsValidYear(year, out int y))
        {
            onFail?.Invoke(CalendarParseFailReason.InvalidYear);
            didFail = true;
        }
        else onSuccess?.Invoke(CalendarParseSuccessReason.Year);

        // Month (1-12)
        if (!IsValidMonth(month, out int m))
        {
            onFail?.Invoke(CalendarParseFailReason.InvalidMonth);
            didFail = true;
        }
        else onSuccess?.Invoke(CalendarParseSuccessReason.Month);

        // Day (1..DaysInMonth)
        if (!IsValidDay(day, m, y, out int d))
        {
            onFail?.Invoke(CalendarParseFailReason.InvalidDay);
            didFail = true;
        }
        else onSuccess?.Invoke(CalendarParseSuccessReason.Day);

        // Hour (0-23)
        if (!IsValidHour(hour, out int h))
        {
            onFail?.Invoke(CalendarParseFailReason.InvalidHour);
            didFail = true;
        }
        else onSuccess?.Invoke(CalendarParseSuccessReason.Hour);

        // Minute (0-59)
        if (!IsValidMinute(minute, out int min))
        {
            onFail?.Invoke(CalendarParseFailReason.InvalidMinute);
            didFail = true;
        }
        else onSuccess?.Invoke(CalendarParseSuccessReason.Minute);

        // Second (supports decimals as milliseconds)
        if (!IsValidSecond(second, out double sec))
        {
            onFail?.Invoke(CalendarParseFailReason.InvalidSecond);
            didFail = true;
        }
        else onSuccess?.Invoke(CalendarParseSuccessReason.Second);

        // Build UTC time
        if (!TryBuildUTCTime(y, m, d, h, min, sec, out dateTime, stripUTC: stripUTC))
        {
            onFail?.Invoke(CalendarParseFailReason.BuildUtcFailed);
            didFail = true;
        }
        else onSuccess?.Invoke(CalendarParseSuccessReason.BuildUtc);

        return !didFail;
    }

    public static bool TryParseJulianDay(string julianDay, out string dateTime, bool isModified = false, Action onFail = null, bool stripUTC = false)
    {
        dateTime = default;
        if (string.IsNullOrWhiteSpace(julianDay))
        {
            onFail?.Invoke();
            Debug.LogWarning("Julian Day input is null/empty.");
            return false;
        }
        if (!double.TryParse(julianDay, NumberStyles.Float, CultureInfo.InvariantCulture, out double jdDouble))
        {
            onFail?.Invoke();
            Debug.LogWarning($"Invalid Julian Day input '{julianDay}'");
            return false;
        }

        if (isModified) jdDouble += JD_MINUS_MJD;

        if (double.IsNaN(jdDouble) || double.IsInfinity(jdDouble))
        {
            onFail?.Invoke();
            Debug.LogWarning($"Julian Day input '{julianDay}' produced NaN/Infinity.");
            return false;
        }

        double seconds = (jdDouble - JD_UNIX_EPOCH) * 86400.0;

        double ticksDouble = seconds * TimeSpan.TicksPerSecond;
        if (ticksDouble < long.MinValue || ticksDouble > long.MaxValue)
        {
            onFail?.Invoke();
            Debug.LogWarning($"Julian Day '{julianDay}' is out of supported range for DateTimeOffset.");
            return false;
        }

        long ticks = (long)Math.Round(ticksDouble, MidpointRounding.AwayFromZero);

        var unixEpoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

        // Build UTC time
        if (!TryBuildUTCTime(unixEpoch, ticks, out dateTime, stripUTC: stripUTC))
        {
            onFail?.Invoke();
            return false;
        }

        return true;

    }

}
