using Unity.Mathematics;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Globalization;
using Mono.Cecil.Cil;


public enum ParsableData
{
    NameID,
    Mass,
    CenterNameID,
    Positions,
    Velocities,
    Radius,
    StartTime,
    StopTime,
    StepSize,

}

public static class HorizonsParser
{
    static HorizonsResponse _response;
    static string _bodyName;
    static string _bodyId;
    static string _centerBodyName;
    static string _centerBodyId;
    static double _masskg;
    static double _radius;
    static double3[] _positions;
    static double3[] _velocities;

    public static bool TryParseData(ParsableData[] parsableData, string[] formattedText, out string[] targetDataValue)
    {
        targetDataValue = Array.Empty<string>();

        if (parsableData == null || parsableData.Length == 0) return false;
        if (formattedText == null || formattedText.Length == 0) return false;

        var keyValuePair = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in formattedText)
        {
            int colon = line.IndexOf(':');
            int equals = line.IndexOf('=');
            string key = string.Empty;
            string value = string.Empty;

            if (colon >= 0 || equals >= 0)
            {
                if (colon >= 0)
                {
                    key = line[..colon];
                    value = line[(colon + 1)..];
                }
                else if (equals >= 0)
                {
                    key = line[..equals];
                    value = line[(equals + 1)..];

                }

            }
            else continue;

            // If duplicates exist, decide policy: keep first or overwrite.
            if (!keyValuePair.ContainsKey(key))
                keyValuePair[key] = value;
        }

        var results = new List<string>(parsableData.Length);

        foreach (var p in parsableData)
        {
            string key = ParsableDataToString(p);

            if (!keyValuePair.TryGetValue(key, out var value))
                results.Add($"{p}: {value}");
        }

        targetDataValue = results.ToArray();
        return targetDataValue.Length > 0;
    }


    public static string[] FormatResponse(HorizonsResponse response, out List<EphemerisSample> formattedVectors, bool lowercase = false)
    {
        string[] splitResults = response?.result?.Replace(" ", "").Split('\n');
        List<string> formattedBodyData = new();
        formattedVectors = new();

        int bodyDataStartIndex = -1;
        int bodyDataEndIndex = -1;
        int ephemerisVectorsStartIndex = -1;
        int ephemerisVectorsEndIndex = -1;

        for (int i = 0; i < splitResults.Length; i++)
        {
            if (splitResults[i].Contains("GEOPHYSICALPROPERTIES"))
            {
                bodyDataStartIndex = i;
            }
            if (splitResults[i].Contains("HELIOCENTRICORBITCHARACTERISTICS"))
            {
                bodyDataEndIndex = i;
            }
            if (splitResults[i].Contains("$$SOE"))
            {
                ephemerisVectorsStartIndex = i;
            }
            if (splitResults[i].Contains("$$EOE"))
            {
                ephemerisVectorsEndIndex = i;
            }
        }

        // Object Data Formatting 
        for (int i = bodyDataStartIndex; i < bodyDataEndIndex; i++) // Iterate over every string element between "GEOPHYSICALPROPERTIES" and "HELIOCENTRICORBITCHARACTERISTICS"
        {
            string row = splitResults[i];
            if (string.IsNullOrEmpty(row) || row.Length < 2)
            {
                formattedBodyData?.Add(row ?? string.Empty);
                continue;
            }

            int splitIndex = -1;

            for (int z = 0; z < row.Length - 1; z++) // Iterate over every character in one string element
            {
                char current = splitResults[i][z];
                char next = splitResults[i][z + 1];

                if (char.IsDigit(current) && char.IsLetter(next)) // If the current character is a digit AND the next character is a letter:
                {
                    splitIndex = z + 1;
                    break;
                }
            }

            if (splitIndex == -1)
            {
                formattedBodyData?.Add(row);
            }
            else
            {
                formattedBodyData?.Add(row[..splitIndex]);
                formattedBodyData?.Add(row[splitIndex..]);
            }

        }

        // Object Ephemeris Formatting
        for (int i = ephemerisVectorsStartIndex + 1; i < ephemerisVectorsEndIndex; i += 3) // Iterate over each ephemeris date, which contains position and velocity vectors
        {
            int dateIndex = -1;
            int posIndex = -1;
            int velIndex = -1;

            // Find Index of [Date], [Position], [Velocity]
            if (splitResults[i].Contains("TDB"))
            {
                dateIndex = i;
                posIndex = i + 1;
                velIndex = i + 2;
            }

            if (TryParseVectors(splitResults[dateIndex], splitResults[posIndex], splitResults[velIndex], out EphemerisSample ephemeris))
                formattedVectors?.Add(ephemeris);
        }

        for (int i = bodyDataEndIndex; i < splitResults.Length; i++)
        {
            formattedBodyData?.Add(splitResults[i]);
        }

        if (lowercase)
            for (int i = 0; i < splitResults.Length; i++)
                splitResults[i] = splitResults[i].ToLower();

        return formattedBodyData?.ToArray();
    }

    static string ParsableDataToString(ParsableData parsableData)
    {
        return parsableData switch
        {
            ParsableData.NameID => "targetbodyname",
            ParsableData.Mass => "massx10^",
            ParsableData.CenterNameID => "centerbodyname",
            ParsableData.Radius => "vol.meanradius(km)",
            ParsableData.StartTime => "starttime",
            ParsableData.StopTime => "stoptime",
            ParsableData.StepSize => "step-size",
            ParsableData.Positions => "",
            ParsableData.Velocities => "",
            _ => "Invalid Parsable Data",
        };
    }

    // Position Vector Format Example: "X=-3.271541395225262E+07Y=1.428771211383301E+08Z=1.914442883169651E+04"
    // Velocity Vector Format Example: "XV=-3.271541395225262E+07YV=1.428771211383301E+08ZV=1.914442883169651E+04"
    static bool TryParseVectors(string date, string positions, string velocities, out EphemerisSample ephemeris)
    {
        ephemeris = default;

        // Find start index of position labels
        int xPosIdx = positions.IndexOf("X=");
        int yPosIdx = positions.IndexOf("Y=");
        int zPosIdx = positions.IndexOf("Z=");
        int posValuesIdx = "X=".Length; // The label's position value index offset

        // Find start index of velocity labels
        int xVelIdx = velocities.IndexOf("XV=");
        if (xVelIdx < 0) xVelIdx = velocities.IndexOf("VX=");
        int yVelIdx = velocities.IndexOf("YV=");
        if (yVelIdx < 0) yVelIdx = velocities.IndexOf("VY=");
        int zVelIdx = velocities.IndexOf("ZV=");
        if (zVelIdx < 0) zVelIdx = velocities.IndexOf("VZ=");
        int velValuesIdx = "XV=".Length; // The label's velocity value index offset

        // Guard against non-valid index
        if (xPosIdx < 0 || yPosIdx < 0 || zPosIdx < 0 || xVelIdx < 0 || yVelIdx < 0 || zVelIdx < 0) return false;

        var posLabelsIdxValue = new (char Axis, int Idx)[]
        {
            ('X', xPosIdx),
            ('Y', yPosIdx),
            ('Z', zPosIdx),
        };
        Array.Sort(posLabelsIdxValue, (a, b) => a.Idx.CompareTo(b.Idx)); // Sort 'posLabelsIdx' ascending by Idx

        var velLabelsIdxValue = new (char Axis, int Idx)[]
        {
            ('X', xVelIdx),
            ('Y', yVelIdx),
            ('Z', zVelIdx),
        };
        Array.Sort(velLabelsIdxValue, (a, b) => a.Idx.CompareTo(b.Idx)); // Sort 'velLabelsIdx' ascending by Idx

        var firstPos = posLabelsIdxValue[0];
        var secondPos = posLabelsIdxValue[1];
        var thirdPos = posLabelsIdxValue[2];

        var firstVel = velLabelsIdxValue[0];
        var secondVel = velLabelsIdxValue[1];
        var thirdVel = velLabelsIdxValue[2];

        // Slice without allocating new strings
        int p1Start = firstPos.Idx + posValuesIdx;
        int p1Len = secondPos.Idx - p1Start;
        ReadOnlySpan<char> rawPos1 = positions.AsSpan(p1Start, p1Len);
        int p2Start = secondPos.Idx + posValuesIdx;
        int p2Len = thirdPos.Idx - p2Start;
        ReadOnlySpan<char> rawPos2 = positions.AsSpan(p2Start, p2Len);
        int p3Start = thirdPos.Idx + posValuesIdx;
        ReadOnlySpan<char> rawPos3 = positions.AsSpan(p3Start);

        int v1Start = firstVel.Idx + velValuesIdx;
        int v1Len = secondVel.Idx - v1Start;
        ReadOnlySpan<char> rawVel1 = velocities.AsSpan(v1Start, v1Len);
        int v2Start = secondVel.Idx + velValuesIdx;
        int v2Len = thirdVel.Idx - v2Start;
        ReadOnlySpan<char> rawVel2 = velocities.AsSpan(v2Start, v2Len);
        int v3Start = thirdVel.Idx + velValuesIdx;
        ReadOnlySpan<char> rawVel3 = velocities.AsSpan(v3Start);

        if (!TryBuildDouble3(firstPos, rawPos1, secondPos, rawPos2, thirdPos, rawPos3, out double3 pos)) return false;
        if (!TryBuildDouble3(firstVel, rawVel1, secondVel, rawVel2, thirdVel, rawVel3, out double3 vel)) return false;

        if (!TryParseJD(rawDate: date, out double dateJD) || !TryBuildDateTime(dateDouble: dateJD, dateTime: out DateTimeOffset dateTime)) return false;

        ephemeris = new(
            date: dateTime,
            position: pos,
            velocity: vel
        );

        return true;
    }

    // Julian Date string/double expected format: 2460676.500000000
    static bool TryBuildDateTime(out DateTimeOffset dateTime, double dateDouble)
    {
        dateTime = default;
        if (!IsValidJulianDayTDB(dateDouble)) return false;

        const double JD_UNIX_EPOCH = 2440587.5; // 1970-01-01 00:00:00 UTC
        double daysSinceUnix = dateDouble - JD_UNIX_EPOCH;

        long ticks = (long)Math.Round(daysSinceUnix * TimeSpan.TicksPerDay);

        dateTime = DateTimeOffset.UnixEpoch.AddTicks(ticks);

        return true;
    }

    static bool TryParseJD(string rawDate, out double doubleJD)
    {
        doubleJD = default;
        const int JD_FORMAT_LENGTH = 17;

        if (rawDate.Length < JD_FORMAT_LENGTH || string.IsNullOrWhiteSpace(rawDate)) return false;
        rawDate = rawDate.Trim();

        int equalsIdx = rawDate.IndexOf('=');
        if (equalsIdx < 0) return false;

        string jdDate = rawDate[..equalsIdx];
        // Debug.Log($"Raw string JD Date: {jdDate}");
        if (jdDate.Length != JD_FORMAT_LENGTH) return false;

        ReadOnlySpan<char> spanJDDate = new(jdDate.ToCharArray());
        if (!double.TryParse(spanJDDate, NumberStyles.Float, CultureInfo.InvariantCulture, out doubleJD))
        {
            // Debug.Log($"Converted <string> -> <double> JD Date: {doubleJD}");
            return false;
        }
        Debug.Log($"Converted <string> -> <double> JD Date: {doubleJD}");

        return true;
    }

    static bool IsValidJulianDayTDB(double jd)
    {
        if (double.IsNaN(jd) || double.IsInfinity(jd)) return false;

        return jd > 0 && jd < 10_000_000;
    }

    static bool TryParseDoubleInvariant(ReadOnlySpan<char> s, out double value)
    {
        s = s.Trim();
        if (s.Length > 0 && s[^1] == ',') s = s[..^1];

        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (c == 'D' || c == 'd')
            {
                var tmp = s.ToString().Replace('D', 'E').Replace('d', 'E');
                return double.TryParse(
                        tmp,
                        NumberStyles.Float | NumberStyles.AllowLeadingSign,
                        CultureInfo.InvariantCulture, out value
                        );
            }
        }

        return double.TryParse(s, NumberStyles.Float | NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture, out value);
    }

    static bool TryBuildDouble3(
     (char Axis, int Idx) a1, ReadOnlySpan<char> v1,
     (char Axis, int Idx) a2, ReadOnlySpan<char> v2,
     (char Axis, int Idx) a3, ReadOnlySpan<char> v3,
     out double3 result)
    {
        result = default;

        if (!TryParseDoubleInvariant(v1, out var d1)) return false;
        if (!TryParseDoubleInvariant(v2, out var d2)) return false;
        if (!TryParseDoubleInvariant(v3, out var d3)) return false;

        double x = 0, y = 0, z = 0;

        static void Assign(ref double x, ref double y, ref double z, char axis, double val)
        {
            switch (axis)
            {
                case 'X': x = val; break;
                case 'Y': y = val; break;
                case 'Z': z = val; break;
            }
        }

        Assign(ref x, ref y, ref z, a1.Axis, d1);
        Assign(ref x, ref y, ref z, a2.Axis, d2);
        Assign(ref x, ref y, ref z, a3.Axis, d3);

        result = new double3(x, y, z);
        return true;
    }


}
