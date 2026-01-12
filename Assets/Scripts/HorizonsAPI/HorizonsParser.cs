using Unity.Mathematics;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEngine.Rendering;
using System.Runtime.InteropServices.WindowsRuntime;

public static class HorizonsParser
{
    public static bool TryParseData(ParsableData[] parsableData, string[] formattedText, out string[] targetParsableDataValue)
    {
        targetParsableDataValue = Array.Empty<string>();

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

        targetParsableDataValue = results.ToArray();
        return targetParsableDataValue.Length > 0;
    }

    static bool TryParseNumericValueFrom(string rawTextValue, out double value)
    {
        if (rawTextValue.EndsWith("km/s", StringComparison.OrdinalIgnoreCase) || rawTextValue.EndsWith("km/d", StringComparison.OrdinalIgnoreCase))
            rawTextValue = rawTextValue[0..^4];

        if (rawTextValue.EndsWith("bar", StringComparison.OrdinalIgnoreCase))
            rawTextValue = rawTextValue[0..^3];

        if (rawTextValue.EndsWith("kg", StringComparison.OrdinalIgnoreCase) || rawTextValue.EndsWith("km", StringComparison.OrdinalIgnoreCase))
            rawTextValue = rawTextValue[0..^2];

        if (rawTextValue.EndsWith("d", StringComparison.OrdinalIgnoreCase) || rawTextValue.EndsWith("y", StringComparison.OrdinalIgnoreCase))
            rawTextValue = rawTextValue[0..^1];


        if (double.TryParse(rawTextValue, out value)) return true;

        int specialCharIdx = rawTextValue.IndexOf("+-");
        if (specialCharIdx > -1)
        {
            string rawValue = rawTextValue[..specialCharIdx];
            if (!double.TryParse(rawValue, out value)) return false;
        }

        specialCharIdx = rawTextValue.IndexOf('^');
        if (specialCharIdx > -1)
        {
            int multiIdx = rawTextValue.IndexOf('x', StringComparison.OrdinalIgnoreCase);
            if (multiIdx < 0) return false;
            if (!double.TryParse(rawTextValue[..multiIdx], out double baseNum)) return false;
            if (!double.TryParse(rawTextValue[(specialCharIdx + 1)..], out double exponent)) return false;

            value = baseNum * (Math.Pow(10.0, exponent));
        }

        return true;
    }

    public static List<string> FormatResponse(HorizonsResponse response, bool removeUnits = false)
    {
        string[] r = response?.result?.Trim().Split('\n', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
        List<string> rawResponse = r.ToList();

        Regex headerThenTail = new(@"^(?<header>[^=]+?:)\s{2,}(?<tail>.+)$", RegexOptions.Compiled);
        Regex pairRegex = new(@"(?<key>[^=:]+?)\s*(?<sep>=|:)\s*(?<value>.+?)(?=\s{2,}[^=:]+?\s*(?:=|:)|$)", RegexOptions.Compiled);

        List<string> formatted = new(rawResponse.Count * 2);

        foreach (var rawLine in rawResponse)
        {
            var line = rawLine.Trim();

            int exceptionIdx = line.IndexOf("Mass Layers", StringComparison.OrdinalIgnoreCase);
            if (exceptionIdx >= 0) line = line[exceptionIdx..].TrimEnd();

            exceptionIdx = line.IndexOf("Surface Area", StringComparison.OrdinalIgnoreCase);
            if (exceptionIdx >= 0) line = line[exceptionIdx..].TrimEnd();

            if (string.IsNullOrWhiteSpace(line)) continue;

            var hm = headerThenTail.Match(line);
            if (hm.Success)
            {
                var header = hm.Groups["header"].Value.Trim();
                var tail = hm.Groups["tail"].Value.Trim();

                if (tail.Contains('=') || tail.Contains(':'))
                {
                    formatted.Add(header);
                    AddPairsOrFallback(tail, formatted, pairRegex);
                    continue;
                }
            }

            AddPairsOrFallback(line, formatted, pairRegex);
        }

        if (removeUnits)
        {
            foreach (string line in formatted)
            {

            }
        }

        return formatted;

    }

    static bool TryRemoveUnits(string rawLine)
    {
        List<string> unitsList = new() { "km/s", "kg", "km", "bar", "gauss", "y", "d" };

        return true;
    }

    static void AddPairsOrFallback(string text, List<string> output, Regex pairRegex)
    {
        var matches = pairRegex.Matches(text);

        if (matches.Count == 0)
        {
            output.Add(text);
            return;
        }

        if (matches.Count == 1)
        {
            output.Add(text);
            return;
        }

        foreach (Match m in matches)
        {
            var key = m.Groups["key"].Value.Trim();
            var sep = m.Groups["sep"].Value;
            var value = m.Groups["value"].Value.Trim();

            if (key.Length == 0 || value.Length == 0)
                continue;

            output.Add($"{key} {sep} {value}");
        }
    }

    public static Dictionary<ParsableData, ParsableDataValue> ParseData(List<string> rawFormattedLines, out List<EphemerisSample> formattedVectors, bool lowercase = false)
    {
        Dictionary<ParsableData, ParsableDataValue> formattedBodyData = new();
        formattedVectors = new();

        int equalsDataStartIndex = -1;
        int equalsEndIndex = -1;
        int ephemerisVectorsStartIndex = -1;
        int ephemerisVectorsEndIndex = -1;
        int colonDataStartIndex = -1;
        int colonDataEndIndex = -1;

        for (int i = 0; i < rawFormattedLines.Count; i++)
        {
            if (rawFormattedLines[i].Contains("GEOPHYSICALPROPERTIES", StringComparison.OrdinalIgnoreCase))
            {
                equalsDataStartIndex = i;
            }
            if (rawFormattedLines[i].Contains("TARGETBODYNAME", StringComparison.OrdinalIgnoreCase))
            {
                equalsEndIndex = i;
                colonDataStartIndex = i;
            }
            if (rawFormattedLines[i].Contains("REFERENCEFRAME", StringComparison.OrdinalIgnoreCase))
            {
                colonDataEndIndex = i + 1;
            }

            if (rawFormattedLines[i].Contains("$$SOE", StringComparison.OrdinalIgnoreCase))
            {
                ephemerisVectorsStartIndex = i;
            }
            if (rawFormattedLines[i].Contains("$$EOE", StringComparison.OrdinalIgnoreCase))
            {
                ephemerisVectorsEndIndex = i;
            }
        }

        // Object Data Formatting
        // Format Data with '='
        for (int i = equalsDataStartIndex; i < equalsEndIndex; i++) // Iterate over every string element between "GEOPHYSICALPROPERTIES" and "HELIOCENTRICORBITCHARACTERISTICS"
        {
            string row = rawFormattedLines[i];
            if (string.IsNullOrEmpty(row) || row.Length < 2) continue;

            for (int z = 0; z < row.Length - 1; z++) // Iterate over every character in one string element
            {
                if (row.Contains('='))
                {
                    int separatorIdx = row.IndexOf('=');

                    string rawKey = row[..separatorIdx];
                    string rawValue = row[(separatorIdx + 1)..];

                    foreach (ParsableData key in Enum.GetValues(typeof(ParsableData)))
                    {
                        if (rawKey.Contains(ParsableDataToString(key), StringComparison.OrdinalIgnoreCase) && !formattedBodyData.ContainsKey(key))
                        {
                            if (!TryParseNumericValueFrom(rawValue, out double numericValue)) numericValue = 0.0;

                            ParsableDataValue ParsableDataValue = new(
                                rawTextValue: rawValue,
                                numericValue: numericValue
                            );

                            formattedBodyData.Add(key, ParsableDataValue);
                        }

                    }

                }
            }
        }

        // Format Data with ':'
        for (int i = colonDataStartIndex; i < colonDataEndIndex; i++)
        {
            string row = rawFormattedLines[i];
            if (string.IsNullOrEmpty(row) || row.Length < 2) continue;

            for (int z = 0; z < row.Length - 1; z++)
            {
                if (row.Contains(":"))
                {
                    int separatorIdx = row.IndexOf(':');

                    string rawKey = row[..separatorIdx];
                    string rawValue = row[..separatorIdx];

                    foreach (ParsableData key in Enum.GetValues(typeof(ParsableData)))
                    {
                        if (rawKey.Contains(ParsableDataToString(key), StringComparison.OrdinalIgnoreCase) && !formattedBodyData.ContainsKey(key))
                        {
                            if (!TryParseNumericValueFrom(rawValue, out double numericValue)) numericValue = 0.0;

                            ParsableDataValue ParsableDataValue = new(
                                rawTextValue: rawValue,
                                numericValue: numericValue
                            );

                            formattedBodyData.Add(key, ParsableDataValue);
                        }

                    }
                }
            }
        }

        // Object Ephemeris Formatting
        for (int i = ephemerisVectorsStartIndex + 1; i < ephemerisVectorsEndIndex; i += 3) // Iterate over each ephemeris date, which contains position and velocity vectors
        {
            int dateIndex = -1;
            int posIndex = -1;
            int velIndex = -1;

            // Find Index of [Date], [Position], [Velocity]
            if (rawFormattedLines[i].Contains("TDB"))
            {
                dateIndex = i;
                posIndex = i + 1;
                velIndex = i + 2;
            }
            else continue;

            if (TryParseVectors(rawFormattedLines[dateIndex], rawFormattedLines[posIndex], rawFormattedLines[velIndex], out double3 positions, out double3 velocities, out DateTimeOffset date))
            {
                formattedVectors?.Add(new(
                        bodyName: formattedBodyData[ParsableData.TargetBodyName].RawTextValue,
                        centerbodyname: formattedBodyData[ParsableData.CenterBodyName].RawTextValue,
                        date: date,
                        position: positions,
                        velocity: velocities
                    ));
            }
        }

        if (lowercase)
            for (int i = 0; i < rawFormattedLines.Count; i++)
                rawFormattedLines[i] = rawFormattedLines[i].ToLower();

        return formattedBodyData;
    }

    static bool HasKeyValuePair(string rawLine, out int separatorIdx)
    {
        rawLine = rawLine.Replace(" ", "");
        separatorIdx = rawLine.IndexOf("=") > -1 ? rawLine.IndexOf("=") : rawLine.IndexOf(":");
        string rawValue = "";

        if (separatorIdx > -1) rawValue = rawLine[(separatorIdx + 1)..].Trim();
        else
        {
            Debug.LogError($"[HorizonsParser] HasKeyValuePair(): Could not locate identifiers ':' or '=' in '{rawLine}'");
            return false;
        }

        if (rawLine.Length < 2 || rawValue.Length <= 0) return false;

        return true;
    }

    static bool TryParseUncertaintyValue(string rawLine, out Dictionary<ParsableData, ParsableDataValue> keyValuePair)
    {
        keyValuePair = new();
        if (!HasKeyValuePair(rawLine, out int separatorIdx))
        {
            Debug.LogError($"[HorizonsParser] TryParseUncertainty(): '{rawLine}' has no valid Key/Value pair");
            return false;
        }
        int uncertaintyIdx = rawLine.IndexOf("+-");
        if (uncertaintyIdx < 0)
        {
            Debug.LogError($"[HorizonsParser] TryParseUncertainty(): '{rawLine}' does not contain an uncertainty character '+-'");
            return false;
        }

        rawLine = rawLine.Replace(" ", "").Trim();
        string rawKey = rawLine[..separatorIdx];
        string rawValue = rawLine[(separatorIdx + 1)..];
        uncertaintyIdx = rawValue.IndexOf("+-");
        string removedUncertaintyValue = rawValue[..uncertaintyIdx];

        if (!double.TryParse(removedUncertaintyValue, out double numericValue))
        {
            Debug.LogError($"[HorizonsParser] TryParseUncertainty(): Could not parse a double from '{removedUncertaintyValue}'");
            return false;
        }

        ParsableData parsedKey = default;
        foreach (ParsableData key in Enum.GetValues(typeof(ParsableData)))
        {
            if (!rawKey.Contains(ParsableDataToString(key), StringComparison.OrdinalIgnoreCase)) continue;
            parsedKey = key;
        }

        ParsableDataValue parsedValue = new(
            rawTextValue: rawValue,
            numericValue: numericValue
            );

        keyValuePair.Add(parsedKey, parsedValue);

        return true;
    }

    static string UnitMeasurementsToString(UnitMeasurements unit)
    {
        return unit switch
        {
            UnitMeasurements.KM_S => "km/s",
            UnitMeasurements.GAUSS_RP3 => "gaussRp^3",
            UnitMeasurements.G_CM3 => "g/cm^3",
            UnitMeasurements.M_S2 => "m/s^2",
            UnitMeasurements.KM3_S2 => "km^3/s^2",
            UnitMeasurements.KM_3 => "km^3",
            UnitMeasurements.DEG_D => "deg/d",
            _ => "Invalid unit measurement"
        };
    }

    static string ParsableDataToString(ParsableData parsableData)
    {
        return parsableData switch
        {
            ParsableData.VolMeanRadius => "vol.meanradius",
            ParsableData.Mass => "massx10^",
            ParsableData.EquatorialRadius => "equ.radius",
            ParsableData.PolarAxis => "polaraxis",
            ParsableData.AtmosMass => "atmos",
            ParsableData.Flattening => "flattening",
            ParsableData.Oceans => "oceans",
            ParsableData.Density => "density",
            ParsableData.Crust => "crust",
            ParsableData.J2_IERS2010 => "j2(iers2010)",
            ParsableData.Mantle => "mantle",
            ParsableData.Surface_G_Polar => "g_p",

            ParsableData.Outercore => "outercore",
            ParsableData.Surface_G_Equatorial => "g_e",
            ParsableData.Innercore => "innercore",
            ParsableData.Standard_G => "g_o",
            ParsableData.FluidcoreRad => "fluidcorerad",
            ParsableData.Geocentric_G => "gm",
            ParsableData.InnercoreRad => "innercorerad",
            ParsableData.Geocentric_G_1Sig => "gm1-sigma",
            ParsableData.EscapeVelocity => "escapevelocity",
            ParsableData.RotationRate => "rot.rate",

            ParsableData.MeanSiderealDay => "meansiderealdy",
            ParsableData.Land => "land",
            ParsableData.Sea => "sea",
            ParsableData.MeanSolarDay2000 => "meansolarday2000",
            ParsableData.MeanSolarDay1820 => "meansolarday1820",

            ParsableData.LoveNo => "loveno.",
            ParsableData.MomentofInertia => "momentofinertia",
            ParsableData.AtmosPressure => "atm.pressure",
            ParsableData.MeanSurfaceTemp_TS => "meansurfacetemp(ts)",
            ParsableData.Volume => "volume",
            ParsableData.MeanEffectTemp_TE => "meaneffect.temp(te)",
            ParsableData.MagneticMoment => "magneticmoment",
            ParsableData.GeometricAlbedo => "geometricalbedo",
            ParsableData.VisMag => "vis.mag.",
            ParsableData.SolarConstant => "solarconstant",

            ParsableData.ObliquityToOrbit => "obliquitytoorbit",
            ParsableData.SiderealOrbPeriod => "siderealorbperiod",
            ParsableData.OrbitalSpeed => "orbitalspeed",
            ParsableData.MeanDailyMotion => "meandailymotion",
            ParsableData.HillsSphereRadius => "hill'ssphereradius",

            ParsableData.TargetBodyName => "targetbodyname",
            ParsableData.TargetBodyID => "targetbodyname",
            ParsableData.CenterBodyName => "centerbodyname",
            ParsableData.CenterBodyID => "centerbodyname",
            ParsableData.CenterSiteName => "center-sitename",
            ParsableData.StartTime => "starttime",
            ParsableData.StopTime => "stoptime",
            ParsableData.StepSize => "step-size",

            ParsableData.CenterGeodetic => "centergeodetic",
            ParsableData.CenterCylindric => "centercylindric",
            ParsableData.CenterRadii => "centerradii",
            ParsableData.OutputUnits => "outputunits",
            ParsableData.CalendarMode => "calendarmode",
            ParsableData.OutputType => "outputtype",
            ParsableData.OutputFormat => "outputformat",
            ParsableData.ReferenceFrame => "referenceframe",

            _ => "invalid parsable data",
        };
    }


    // Position Vector Format Example: "X=-3.271541395225262E+07Y=1.428771211383301E+08Z=1.914442883169651E+04"
    // Velocity Vector Format Example: "XV=-3.271541395225262E+07YV=1.428771211383301E+08ZV=1.914442883169651E+04"
    static bool TryParseVectors(string date, string positions, string velocities, out double3 position, out double3 velocity, out DateTimeOffset dateTime)
    {
        position = default;
        velocity = default;
        dateTime = default;

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

        if (!TryParseJD(rawDate: date, out double dateJD) || !TryBuildDateTime(dateDouble: dateJD, out dateTime)) return false;

        position = pos;
        velocity = vel;

        return true;
    }

    // Julian Date string/double expected format: 2460676.500000000
    static bool TryBuildDateTime(double dateDouble, out DateTimeOffset dateTime)
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
