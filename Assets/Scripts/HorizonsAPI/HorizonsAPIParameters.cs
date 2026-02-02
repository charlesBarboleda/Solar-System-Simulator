using System;
using System.Globalization;
using UnityEngine;

public static class HorizonsAPIParameters
{
    // Julian Day & Modified constants
    const double JD_UNIX_EPOCH = 2440587.5;
    const double JD_MINUS_MJD = 2400000.5;
    public static bool TryParseJulianDay(string julianDay, out string dateTime, bool isModified = false)
    {
        dateTime = default;
        if (string.IsNullOrWhiteSpace(julianDay))
        {
            Debug.LogWarning("Julian Day input is null/empty.");
            return false;
        }
        if (!double.TryParse(julianDay, NumberStyles.Float, CultureInfo.InvariantCulture, out double jdDouble))
        {
            Debug.LogWarning($"Invalid Julian Day input '{julianDay}'");
            return false;
        }

        if (isModified) jdDouble += JD_MINUS_MJD;

        if (double.IsNaN(jdDouble) || double.IsInfinity(jdDouble))
        {
            Debug.LogWarning($"Julian Day input '{julianDay}' produced NaN/Infinity.");
            return false;
        }

        double seconds = (jdDouble - JD_UNIX_EPOCH) * 86400.0;

        double ticksDouble = seconds * TimeSpan.TicksPerSecond;
        if (ticksDouble < long.MinValue || ticksDouble > long.MaxValue)
        {
            Debug.LogWarning($"Julian Day '{julianDay}' is out of supported range for DateTimeOffset.");
            return false;
        }

        long ticks = (long)Math.Round(ticksDouble, MidpointRounding.AwayFromZero);

        var unixEpoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

        // Build UTC time
        try
        {
            DateTimeOffset dto = unixEpoch + TimeSpan.FromTicks(ticks);
            dateTime = dto.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff 'UTC'", CultureInfo.InvariantCulture);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }

    }

    public static bool TryParseCalendarDay(out string dateTime, string year, string month = "1", string day = "1", string hour = "0", string minute = "0", string second = "0")
    {
        dateTime = default;

        // Year
        if (!int.TryParse(year, NumberStyles.Integer, CultureInfo.InvariantCulture, out int y) || y < 0 || y > 9999)
        {
            Debug.LogWarning($"Invalid Year input '{year}'");
            return false;
        }

        // Month (1-12)
        if (!int.TryParse(month, NumberStyles.Integer, CultureInfo.InvariantCulture, out int m) || m < 1 || m > 12)
        {
            Debug.LogWarning($"Invalid Month input '{month}'");
            return false;
        }

        // Day (1..DaysInMonth)
        if (!int.TryParse(day, NumberStyles.Integer, CultureInfo.InvariantCulture, out int d))
        {
            Debug.LogWarning($"Invalid Day input '{day}'");
            return false;
        }
        int maxDay = DateTime.DaysInMonth(y, m);
        if (d < 1 || d > maxDay)
        {
            Debug.LogWarning($"Invalid Day input '{day}' for {y:D4}-{m:D2} (max {maxDay})");
            return false;
        }

        // Hour (0-23)
        if (!int.TryParse(hour, NumberStyles.Integer, CultureInfo.InvariantCulture, out int h) || h < 0 || h > 23)
        {
            Debug.LogWarning($"Invalid Hour input '{hour}'");
            return false;
        }

        // Minute (0-59)
        if (!int.TryParse(minute, NumberStyles.Integer, CultureInfo.InvariantCulture, out int min) || min < 0 || min > 59)
        {
            Debug.LogWarning($"Invalid Minute input '{minute}'");
            return false;
        }

        // Second (supports decimals as milliseconds)
        if (!double.TryParse(second, NumberStyles.Float, CultureInfo.InvariantCulture, out double sec) || sec < 0.0 || sec >= 60.0)
        {
            Debug.LogWarning($"Invalid Second input '{second}'");
            return false;
        }

        // Build UTC time
        try
        {
            var dto = new DateTimeOffset(y, m, d, h, min, 0, TimeSpan.Zero).AddSeconds(sec);

            dateTime = dto.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff 'UTC'", CultureInfo.InvariantCulture);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            Debug.LogWarning($"Calendar date out of range: y={y}, m={m}, d={d}, h={h}, min={min}, sec={sec}");
            return false;
        }
    }
}
