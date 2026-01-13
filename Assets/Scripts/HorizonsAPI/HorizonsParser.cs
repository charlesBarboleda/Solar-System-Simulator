using Unity.Mathematics;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Linq;
using NUnit.Framework;

public static class HorizonsParser
{
    public static List<string> FormatResponse(HorizonsResponse response)
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

        return formatted;

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

    public static Dictionary<ParsableData, ParsableDataValue> ParseBodyData(IReadOnlyList<string> lines)
    {
        var result = new Dictionary<ParsableData, ParsableDataValue>();

        var unitLookup = BuildUnitLookup();

        int bodyDataStartIdx = -1;
        int bodyDataEndIdx = -1;

        for (int i = 0; i < lines.Count; i++)
        {
            string formattedLine = lines[i].Replace(" ", "");
            if (formattedLine.Contains("GeoPhysicalProperties", StringComparison.OrdinalIgnoreCase)) bodyDataStartIdx = i + 1;
            if (formattedLine.Contains("Hill'sSphereRadius", StringComparison.OrdinalIgnoreCase)) bodyDataEndIdx = i + 1;
        }

        for (int i = bodyDataStartIdx; i < bodyDataEndIdx; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
                continue;

            line = line.Trim();

            if (!TrySplitKeyValue(line, out string keyPartRaw, out string valuePartRaw))
                continue;

            if (!TryMatchParsableData(keyPartRaw, out ParsableData parsableData))
                continue;

            int exponentFromKey = ExtractExponent10(keyPartRaw);

            UnitMeasurements unitFromKey = ExtractUnitFromKey(keyPartRaw, unitLookup);

            if (TryParseNumericValue(valuePartRaw, exponentFromKey, unitFromKey, unitLookup, out double numeric, out UnitMeasurements finalUnit))
            {
                string rawNumeric = numeric.ToString("G17", CultureInfo.InvariantCulture);
                result[parsableData] = new ParsableDataValue(rawNumeric, numeric, finalUnit);
            }
            else
            {
                string rawString = valuePartRaw.Trim();
                result[parsableData] = new ParsableDataValue(rawString, rawString);
            }
        }

        return result;

        // -------------------- local helpers --------------------

        static Dictionary<string, UnitMeasurements> BuildUnitLookup()
        {
            var map = new Dictionary<string, UnitMeasurements>(StringComparer.OrdinalIgnoreCase);
            foreach (UnitMeasurements u in Enum.GetValues(typeof(UnitMeasurements)))
            {
                string sig = UnitMeasurementsToString(u);
                sig = NormalizeSig(sig);

                if (!map.ContainsKey(sig))
                    map.Add(sig, u);

                if (u == UnitMeasurements.KM_3 && !map.ContainsKey("km3")) map.Add("km3", u);
                if (u == UnitMeasurements.KM3_S2 && !map.ContainsKey("km3/s2")) map.Add("km3/s2", u);
                if (u == UnitMeasurements.M_S2 && !map.ContainsKey("m/s2")) map.Add("m/s2", u);
            }
            return map;
        }

        static bool TrySplitKeyValue(string line, out string keyPart, out string valuePart)
        {
            int eq = line.IndexOf('=');
            int col = line.IndexOf(':');

            int sep =
                (eq >= 0 && col >= 0) ? Math.Min(eq, col) :
                (eq >= 0) ? eq :
                (col >= 0) ? col :
                -1;

            if (sep < 0)
            {
                keyPart = null;
                valuePart = null;
                return false;
            }

            keyPart = line.Substring(0, sep).Trim();
            valuePart = line.Substring(sep + 1).Trim();
            return !(keyPart.Length == 0);
        }

        static bool TryMatchParsableData(string keyPartRaw, out ParsableData parsableData)
        {
            string keyNorm = NormalizeSig(keyPartRaw);

            ParsableData? match = null;

            foreach (ParsableData pd in Enum.GetValues(typeof(ParsableData)))
            {
                string sig = NormalizeSig(ParsableDataToString(pd));
                if (sig == "invalidparsabledata")
                    continue;

                if (keyNorm.Contains(sig, StringComparison.Ordinal))
                {
                    match = pd;
                    break;
                }
            }

            if (match == null)
            {
                parsableData = default;
                return false;
            }

            if (match == ParsableData.TargetBodyName || match == ParsableData.TargetBodyID)
            {
                parsableData = HasParenDigits(keyPartRaw) ? ParsableData.TargetBodyID : ParsableData.TargetBodyName;
                return true;
            }

            if (match == ParsableData.CenterBodyName || match == ParsableData.CenterBodyID)
            {
                parsableData = HasParenDigits(keyPartRaw) ? ParsableData.CenterBodyID : ParsableData.CenterBodyName;
                return true;
            }

            parsableData = match.Value;
            return true;

            static bool HasParenDigits(string s)
            {
                int l = s.IndexOf('(');
                int r = s.IndexOf(')', l + 1);
                if (l < 0 || r < 0 || r <= l + 1)
                    return false;

                for (int i = l + 1; i < r; i++)
                {
                    if (!char.IsDigit(s[i]))
                        return false;
                }
                return true;
            }
        }

        static int ExtractExponent10(string s)
        {
            string norm = NormalizeSig(s);
            int idx = norm.IndexOf("10^", StringComparison.Ordinal);
            if (idx < 0)
                return 0;

            int i = idx + 3;
            if (i >= norm.Length)
                return 0;

            int sign = 1;
            if (norm[i] == '+') { sign = 1; i++; }
            else if (norm[i] == '-') { sign = -1; i++; }

            int start = i;
            while (i < norm.Length && char.IsDigit(norm[i])) i++;

            if (i == start)
                return 0;

            if (!int.TryParse(norm.Substring(start, i - start), NumberStyles.Integer, CultureInfo.InvariantCulture, out int exp))
                return 0;

            return sign * exp;
        }

        static UnitMeasurements ExtractUnitFromKey(string keyPartRaw, Dictionary<string, UnitMeasurements> unitLookupMap)
        {

            string key = keyPartRaw;

            int l = key.IndexOf('(');
            int r = key.IndexOf(')', l + 1);
            if (l >= 0 && r > l)
            {
                string inside = key.Substring(l + 1, r - l - 1).Trim();
                string insideNorm = NormalizeSig(inside);

                if (unitLookupMap.TryGetValue(insideNorm, out UnitMeasurements u))
                    return u;
            }

            int comma = key.IndexOf(',');
            if (comma >= 0 && comma < key.Length - 1)
            {
                string afterComma = key.Substring(comma + 1).Trim();

                int dl = afterComma.IndexOf('(');
                if (dl >= 0)
                    afterComma = afterComma.Substring(0, dl).Trim();

                string afterCommaNorm = NormalizeSig(afterComma);

                if (unitLookupMap.TryGetValue(afterCommaNorm, out UnitMeasurements u))
                    return u;
            }

            return UnitMeasurements.None;
        }

        static bool TryParseNumericValue(
            string valuePartRaw,
            int exponentFromKey,
            UnitMeasurements unitFromKey,
            Dictionary<string, UnitMeasurements> unitLookupMap,
            out double numeric,
            out UnitMeasurements finalUnit)
        {
            numeric = default;
            finalUnit = unitFromKey;

            string v = valuePartRaw.Trim();

            int pm = v.IndexOf("+-", StringComparison.Ordinal);
            if (pm >= 0)
                v = v.Substring(0, pm).Trim();

            int exponentFromValue = ExtractExponent10(v);

            if (finalUnit == UnitMeasurements.None)
            {
                if (TryExtractSuffixUnit(ref v, unitLookupMap, out UnitMeasurements suffixUnit))
                    finalUnit = suffixUnit;
            }
            else
            {
                _ = TryExtractSuffixUnit(ref v, unitLookupMap, out _);
            }

            if (!TryExtractLeadingNumberToken(v, out string numberToken))
                return false;

            if (!double.TryParse(numberToken, NumberStyles.Float, CultureInfo.InvariantCulture, out double baseValue))
                return false;

            int totalExp = exponentFromKey + exponentFromValue;
            if (totalExp != 0)
                baseValue *= Math.Pow(10d, totalExp);

            numeric = baseValue;
            return true;

            static bool TryExtractSuffixUnit(ref string s, Dictionary<string, UnitMeasurements> unitLookup, out UnitMeasurements unit)
            {
                unit = UnitMeasurements.None;

                string trimmed = s.Trim();
                int lastSpace = trimmed.LastIndexOf(' ');
                if (lastSpace < 0 || lastSpace == trimmed.Length - 1)
                    return false;

                string lastToken = trimmed.Substring(lastSpace + 1).Trim().TrimEnd(',', ';');

                string lastTokenNorm = NormalizeSig(lastToken);

                if (!unitLookup.TryGetValue(lastTokenNorm, out unit))
                    return false;

                s = trimmed.Substring(0, lastSpace).Trim();
                return true;
            }

            static bool TryExtractLeadingNumberToken(string s, out string token)
            {
                token = null;

                int n = s.Length;
                int start = -1;

                for (int i = 0; i < n; i++)
                {
                    char c = s[i];
                    if (char.IsDigit(c) || c == '+' || c == '-' || c == '.')
                    {
                        start = i;
                        break;
                    }
                }

                if (start < 0)
                    return false;

                bool seenE = false;
                bool allowSignAfterE = false;

                int end = start;
                for (; end < n; end++)
                {
                    char c = s[end];

                    if (char.IsDigit(c) || c == '.')
                    {
                        allowSignAfterE = false;
                        continue;
                    }

                    if ((c == 'E' || c == 'e') && !seenE)
                    {
                        seenE = true;
                        allowSignAfterE = true;
                        continue;
                    }

                    if ((c == '+' || c == '-') && allowSignAfterE)
                    {
                        allowSignAfterE = false;
                        continue;
                    }

                    break;
                }

                if (end <= start)
                    return false;

                token = s.Substring(start, end - start);
                return true;
            }
        }

        static string NormalizeSig(string s)
        {
            s = s.ToLowerInvariant();
            Span<char> tmp = s.Length <= 256 ? stackalloc char[s.Length] : new char[s.Length];
            int w = 0;

            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (!char.IsWhiteSpace(c))
                    tmp[w++] = c;
            }

            return new string(tmp.Slice(0, w));
        }
    }

    public static string UnitMeasurementsToString(UnitMeasurements unit)
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
            UnitMeasurements.W_M2 => "w/m^2",
            _ => unit.ToString()
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
