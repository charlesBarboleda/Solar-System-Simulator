using Unity.Mathematics;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Linq;

public readonly struct ParsedPhysicalProperty
{
    public readonly string RawKey;
    public readonly string NormalizedKey;
    public readonly string RawValue;
    public readonly double? NumericValue;
    public readonly UnitMeasurements Unit;

    public ParsedPhysicalProperty(string rawKey, string normalizedKey, string rawValue, double? numericValue, UnitMeasurements unit)
    {
        RawKey = rawKey;
        NormalizedKey = normalizedKey;
        RawValue = rawValue;
        NumericValue = numericValue;
        Unit = unit;
    }
}

public readonly struct PhysicalTraitsEntry
{
    public readonly string ObjectName;
    public readonly string TraitName;
    public readonly ParsableDataValue Value;

    public PhysicalTraitsEntry(
        string objectName,
        string traitName,
        ParsableDataValue value)
    {
        ObjectName = objectName;
        TraitName = traitName;
        Value = value;
    }
}

public readonly struct EphemerisEntry
{
    public readonly string ObjectName;
    public readonly DateTimeOffset DateTime;
    public readonly double3 Position;   // km   – valid when HasPosition
    public readonly double3 Velocity;   // km/s – valid when HasVelocity
    public readonly bool HasPosition;
    public readonly bool HasVelocity;

    public EphemerisEntry(string objectName, DateTimeOffset dt, double3 pos, double3 vel, bool hasPos, bool hasVel)
    {
        ObjectName = objectName;
        DateTime = dt;
        Position = pos;
        Velocity = vel;
        HasPosition = hasPos;
        HasVelocity = hasVel;
    }
}

public static class HorizonsParser
{
    static readonly Regex s_headerThenTail = new(@"^(?<header>[^=]+?:)\s{2,}(?<tail>.+)$", RegexOptions.Compiled);
    static readonly Regex s_pairRegex = new(@"(?<key>[^=:\~]+?)\s*(?<sep>=|:|~)\s*(?<value>.+?)(?=\s{2,}[^=:\~]+?\s*(?:=|:|~)|$)", RegexOptions.Compiled);

    //  PUBLIC
    // Parses a Horizons ephemeris SOE block; Y/Z swapped to match simulation axis convention.
    public static bool TryParseEphemeris(
        HorizonsResponse response,
        RequestTypeSimple requestTypeSimple,
        out List<EphemerisEntry> entries)
    {
        entries = new List<EphemerisEntry>();

        if (requestTypeSimple == RequestTypeSimple.PhysicalTraits)
        {
            Debug.LogWarning("[HorizonsParser] TryParseEphemeris: use FormatResponse + ParseBodyData for PhysicalTraits.");
            return false;
        }

        string raw = response?.result;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        if (!TryExtractSoeLines(raw, out List<string> soeLines)) return false;

        int linesPerEntry = requestTypeSimple == RequestTypeSimple.PosAndVel ? 3 : 2;

        if (soeLines.Count % linesPerEntry != 0)
        {
            Debug.LogWarning($"[HorizonsParser] SOE line count ({soeLines.Count}) not divisible by {linesPerEntry} for {requestTypeSimple}.");
            return false;
        }

        TryParseTargetBodyName(raw, out string targetObjectName);

        for (int i = 0; i < soeLines.Count; i += linesPerEntry)
        {
            if (!TryParseJD(soeLines[i], out double jd)) continue;
            if (!TryBuildDateTime(jd, out DateTimeOffset dt)) continue;

            switch (requestTypeSimple)
            {
                case RequestTypeSimple.Position:
                    {
                        if (!TryParsePosition(StripWhitespace(soeLines[i + 1]), out double3 pos)) continue;
                        (pos.y, pos.z) = (pos.z, pos.y);
                        entries.Add(new EphemerisEntry(targetObjectName, dt, pos, default, true, false));
                        break;
                    }
                case RequestTypeSimple.Velocity:
                    {
                        if (!TryParseVelocity(StripWhitespace(soeLines[i + 1]), out double3 vel)) continue;
                        (vel.y, vel.z) = (vel.z, vel.y);
                        entries.Add(new EphemerisEntry(targetObjectName, dt, default, vel, false, true));
                        break;
                    }
                case RequestTypeSimple.PosAndVel:
                    {
                        if (!TryParsePosition(StripWhitespace(soeLines[i + 1]), out double3 pos)) continue;
                        if (!TryParseVelocity(StripWhitespace(soeLines[i + 2]), out double3 vel)) continue;
                        (pos.y, pos.z) = (pos.z, pos.y);
                        (vel.y, vel.z) = (vel.z, vel.y);
                        entries.Add(new EphemerisEntry(targetObjectName, dt, pos, vel, true, true));
                        break;
                    }
            }
        }

        return entries.Count > 0;
    }

    public static List<string> FormatResponse(HorizonsResponse response)
    {
        string[] r = response?.result?.Trim().Split('\n', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();

        List<string> formatted = new(r.Length * 2);

        foreach (var rawLine in r)
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            line = SplitOutSectionLabel(line, "Mass Layers", formatted);
            line = SplitOutSectionLabel(line, "Surface Area", formatted);

            if (string.IsNullOrWhiteSpace(line)) continue;

            var hm = s_headerThenTail.Match(line);
            if (hm.Success)
            {
                var tail = hm.Groups["tail"].Value.Trim();
                if (tail.Contains('=') || tail.Contains(':'))
                {
                    formatted.Add(hm.Groups["header"].Value.Trim());
                    AddPairsOrFallback(tail, formatted, s_pairRegex);
                    continue;
                }
            }

            AddPairsOrFallback(line, formatted, s_pairRegex);
        }

        return formatted;
    }

    public static Dictionary<string, ParsedPhysicalProperty> ParseBodyData(IReadOnlyList<string> lines)
    {
        var result = new Dictionary<string, ParsedPhysicalProperty>(StringComparer.OrdinalIgnoreCase);

        var unitLookup = BuildUnitLookup();

        int startIdx = -1;
        int endIdx = -1;

        for (int i = 0; i < lines.Count; i++)
        {
            string f = lines[i].Replace(" ", "");

            if (startIdx < 0 && (f.Contains("physicalpropert", StringComparison.OrdinalIgnoreCase) || f.Contains("physicalparam", StringComparison.OrdinalIgnoreCase) || f.Contains("physicaldata", StringComparison.OrdinalIgnoreCase) || f.Contains("satellitephysical", StringComparison.OrdinalIgnoreCase)))
            {
                startIdx = i + 1;
                continue;
            }

            if (startIdx < 0 || i < startIdx) continue;

            if (f.Contains("AsteroidComments", StringComparison.OrdinalIgnoreCase) || f.Contains("HeliocentricOrbit", StringComparison.OrdinalIgnoreCase))
            {
                endIdx = i;
                break;
            }
        }

        if (startIdx >= 0 && endIdx < 0) endIdx = lines.Count;

        if (startIdx < 0 || endIdx <= startIdx)
        {
            Debug.LogError("[HorizonsParser] Failed to locate physical properties section.");
            return result;
        }

        for (int i = startIdx; i < endIdx; i++)
        {
            string line = lines[i]?.Trim();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            MatchCollection matches = s_pairRegex.Matches(line);
            if (matches.Count == 0)
                continue;

            foreach (Match m in matches)
            {
                string keyRaw = m.Groups["key"].Value.Trim();
                string valRaw = m.Groups["value"].Value.Trim();

                if (string.IsNullOrWhiteSpace(keyRaw) || string.IsNullOrWhiteSpace(valRaw))
                    continue;

                string normalizedKey = NormalizePropertyKey(keyRaw);
                string uniqueKey = normalizedKey;
                int duplicateIndex = 2;

                while (result.ContainsKey(uniqueKey))
                {
                    uniqueKey = $"{normalizedKey}_{duplicateIndex}";
                    duplicateIndex++;
                }

                int exp = ExtractExponent10(keyRaw);
                UnitMeasurements unit = ExtractUnitFromKey(keyRaw, unitLookup);

                double? numericValue = null;

                if (TryParseNumericValue(
                    valRaw,
                    exp,
                    unit,
                    unitLookup,
                    out double numeric,
                    out UnitMeasurements finalUnit))
                {
                    numericValue = numeric;
                    unit = finalUnit;
                }

                result[uniqueKey] = new ParsedPhysicalProperty(
                    rawKey: keyRaw,
                    normalizedKey: normalizedKey,
                    rawValue: valRaw,
                    numericValue: numericValue,
                    unit: unit);
            }
        }

        return result;
    }

    public static bool TryParseCatalog(List<string> rawFormattedResponse, out List<BodyCatalog> catalog)
    {
        catalog = new();
        int startIdx = -1, endIdx = -1;

        for (int i = 0; i < rawFormattedResponse.Count; i++)
        {
            string f = rawFormattedResponse[i].Replace(" ", "").Trim();
            if (f.Contains("ID#NameDesignationIAU/aliases/other", StringComparison.OrdinalIgnoreCase)) startIdx = i + 2;
            if (f.Contains("numberofmatches", StringComparison.OrdinalIgnoreCase)) endIdx = i;
        }

        if (startIdx < 0 || endIdx < 0)
        {
            Debug.LogError("[HorizonsParser] TryParseCatalog: could not find start/end indices.");
            return false;
        }

        for (int i = startIdx; i < endIdx; i++)
            if (TryParseCatalogLine(rawFormattedResponse[i], out BodyCatalog c))
                catalog.Add(c);

        return true;
    }
    public static string UnitMeasurementsToString(UnitMeasurements unit) =>
    unit switch
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
    public static bool TryParseTargetBodyName(string raw, out string name)
    {
        name = string.Empty;
        if (string.IsNullOrWhiteSpace(raw)) return false;

        const string marker = "Target body name:";
        int markerIdx = raw.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (markerIdx >= 0)
        {
            int start = markerIdx + marker.Length;
            int end = raw.IndexOf('\n', start);
            string segment = (end < 0 ? raw[start..] : raw[start..end]).Trim();

            int braceIdx = segment.IndexOf('{');
            if (braceIdx >= 0) segment = segment[..braceIdx].Trim();

            segment = Regex.Replace(segment, @"\(\d+\)\s*$", "").Trim();
            segment = Regex.Replace(segment, @"\s+/.*$", "").Trim();

            if (segment.Length > 0)
            {
                name = segment.Trim();
                return true;
            }
        }

        const string revisedMarker = "Revised:";
        int revisedIdx = raw.IndexOf(revisedMarker, StringComparison.OrdinalIgnoreCase);
        if (revisedIdx >= 0)
        {
            int lineEnd = raw.IndexOf('\n', revisedIdx);
            string line = (lineEnd < 0 ? raw[revisedIdx..] : raw[revisedIdx..lineEnd]).Trim();

            string[] tokens = Regex.Split(line, @"\s{2,}").Select(t => t.Trim()).Where(t => t.Length > 0 && !t.StartsWith("/")).ToArray();
            if (tokens.Length >= 2)
            {
                string last = tokens[^1];
                string secondToLast = tokens[^2];
                bool lastIsNaifId = Regex.IsMatch(last, @"^\d[\d\s/\w]*$");

                string candidate = lastIsNaifId ? secondToLast : last;
                if (candidate.Length > 0)
                {
                    candidate = Regex.Replace(candidate.Trim(), @"^\d+\s+", "").Trim();
                    if (candidate.Length > 0 && !char.IsDigit(candidate[0]))
                    {
                        name = Regex.Replace(candidate, @"\s+/.*$", "").Trim();
                        return true;
                    }
                }
            }
        }

        return false;
    }

    //  PRIVATE helpers
    static bool TryExtractSoeLines(string raw, out List<string> lines)
    {
        lines = new List<string>();

        int soeIdx = raw.IndexOf("$$SOE", StringComparison.Ordinal);
        int eoeIdx = raw.IndexOf("$$EOE", StringComparison.Ordinal);
        if (soeIdx < 0 || eoeIdx < 0 || eoeIdx <= soeIdx) return false;

        int blockStart = raw.IndexOf('\n', soeIdx);
        if (blockStart < 0 || blockStart >= eoeIdx) return false;
        blockStart++;

        foreach (string line in raw.Substring(blockStart, eoeIdx - blockStart).Split('\n'))
        {
            string t = line.TrimEnd('\r');
            if (!string.IsNullOrWhiteSpace(t)) lines.Add(t);
        }

        return lines.Count > 0;
    }
    // Input after StripWhitespace: "X=-8.889E+07Y=1.174E+08Z=5.404E+05"
    static bool TryParsePosition(string compact, out double3 pos) =>
        TryParseXYZ(compact, "X=", "Y=", "Z=", 2, out pos);

    // Input after StripWhitespace: "VX=-2.426E+01VY=-1.802E+01VZ=-8.275E-02"; also handles XV=/YV=/ZV= format.
    static bool TryParseVelocity(string compact, out double3 vel)
    {
        bool vxFmt = compact.IndexOf("VX=", StringComparison.Ordinal) >= 0;
        return TryParseXYZ(compact, vxFmt ? "VX=" : "XV=", vxFmt ? "VY=" : "YV=", vxFmt ? "VZ=" : "ZV=", 3, out vel);
    }
    // Finds each axis label, slices numeric tokens between them, assigns regardless of axis ordering.
    static bool TryParseXYZ(string compact, string xL, string yL, string zL, int labelLen, out double3 result)
    {
        result = default;

        int xIdx = compact.IndexOf(xL, StringComparison.Ordinal);
        int yIdx = compact.IndexOf(yL, StringComparison.Ordinal);
        int zIdx = compact.IndexOf(zL, StringComparison.Ordinal);
        if (xIdx < 0 || yIdx < 0 || zIdx < 0) return false;

        var axes = new (char Axis, int Idx)[] { ('X', xIdx), ('Y', yIdx), ('Z', zIdx) };
        Array.Sort(axes, (a, b) => a.Idx.CompareTo(b.Idx));

        int s0 = axes[0].Idx + labelLen, s1 = axes[1].Idx + labelLen, s2 = axes[2].Idx + labelLen;
        if (axes[1].Idx < s0 || axes[2].Idx < s1 || s2 > compact.Length) return false;

        ReadOnlySpan<char> span = compact.AsSpan();
        if (!TryParseDoubleInvariant(span.Slice(s0, axes[1].Idx - s0), out double d0)) return false;
        if (!TryParseDoubleInvariant(span.Slice(s1, axes[2].Idx - s1), out double d1)) return false;
        if (!TryParseDoubleInvariant(span.Slice(s2), out double d2)) return false;

        double x = 0, y = 0, z = 0;
        Assign(ref x, ref y, ref z, axes[0].Axis, d0);
        Assign(ref x, ref y, ref z, axes[1].Axis, d1);
        Assign(ref x, ref y, ref z, axes[2].Axis, d2);
        result = new double3(x, y, z);
        return true;

        static void Assign(ref double x, ref double y, ref double z, char axis, double v)
        {
            switch (axis) { case 'X': x = v; break; case 'Y': y = v; break; case 'Z': z = v; break; }
        }
    }
    static bool TryParseJD(string raw, out double jd)
    {
        jd = default;

        if (string.IsNullOrWhiteSpace(raw)) return false;

        raw = raw.Trim();

        int eq = raw.IndexOf('=');
        if (eq <= 0) return false;

        ReadOnlySpan<char> token = raw.AsSpan(0, eq).Trim();

        if (token.IsEmpty) return false;

        return double.TryParse(token, NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out jd);
    }
    static bool TryBuildDateTime(double jd, out DateTimeOffset dt)
    {
        dt = default;
        if (!IsValidJD(jd)) return false;

        // Convert JD to Julian calendar components directly
        // Using the algorithm from Meeus: Astronomical Algorithms
        double jd0 = jd + 0.5;
        long Z = (long)jd0;
        double F = jd0 - Z;

        long A;
        if (Z < 2299161)
        {
            A = Z;
        }
        else
        {
            long alpha = (long)((Z - 1867216.25) / 36524.25);
            A = Z + 1 + alpha - alpha / 4;
        }

        long B = A + 1524;
        long C = (long)((B - 122.1) / 365.25);
        long D = (long)(365.25 * C);
        long E = (long)((B - D) / 30.6001);

        int day = (int)(B - D - (long)(30.6001 * E));
        int month = (int)(E < 14 ? E - 1 : E - 13);
        int year = (int)(month > 2 ? C - 4716 : C - 4715);

        double dayFraction = F;
        int hour = (int)(dayFraction * 24);
        dayFraction = dayFraction * 24 - hour;
        int minute = (int)(dayFraction * 60);
        dayFraction = dayFraction * 60 - minute;
        int second = (int)(dayFraction * 60);
        int ms = (int)((dayFraction * 60 - second) * 1000);

        try
        {
            dt = new DateTimeOffset(year, month, day, hour, minute, second, ms, TimeSpan.Zero);
            return true;
        }
        catch
        {
            return false;
        }
    }
    static bool IsValidJD(double jd) => !double.IsNaN(jd) && !double.IsInfinity(jd) && jd > 0 && jd < 10_000_000;
    static string StripWhitespace(string s)
    {
        Span<char> buf = s.Length <= 512 ? stackalloc char[s.Length] : new char[s.Length];
        int w = 0;
        foreach (char c in s)
            if (!char.IsWhiteSpace(c)) buf[w++] = c;
        return new string(buf[..w]);
    }
    static bool TryParseDoubleInvariant(ReadOnlySpan<char> s, out double value)
    {
        s = s.Trim();
        if (s.Length > 0 && s[^1] == ',') s = s[..^1];

        for (int i = 0; i < s.Length; i++)
        {
            if (s[i] == 'D' || s[i] == 'd')
            {
                string tmp = s.ToString().Replace('D', 'E').Replace('d', 'E');
                return double.TryParse(tmp, NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
            }
        }

        return double.TryParse(s, NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
    }
    static void AddPairsOrFallback(string text, List<string> output, Regex pairRegex)
    {
        var matches = pairRegex.Matches(text);
        if (matches.Count <= 1) { output.Add(text); return; }

        foreach (Match m in matches)
        {
            var key = m.Groups["key"].Value.Trim();
            var val = m.Groups["value"].Value.Trim();
            if (key.Length > 0 && val.Length > 0)
                output.Add($"{key} {m.Groups["sep"].Value} {val}");
        }
    }
    static Dictionary<string, UnitMeasurements> BuildUnitLookup()
    {
        var map = new Dictionary<string, UnitMeasurements>(StringComparer.OrdinalIgnoreCase);

        foreach (UnitMeasurements u in Enum.GetValues(typeof(UnitMeasurements)))
        {
            string sig = NormalizeSig(UnitMeasurementsToString(u));
            if (!map.ContainsKey(sig)) map.Add(sig, u);
        }

        if (!map.ContainsKey("km3")) map.Add("km3", UnitMeasurements.KM_3);
        if (!map.ContainsKey("km3/s2")) map.Add("km3/s2", UnitMeasurements.KM3_S2);
        if (!map.ContainsKey("m/s2")) map.Add("m/s2", UnitMeasurements.M_S2);

        return map;
    }
    static string SplitOutSectionLabel(string line, string marker, List<string> output)
    {
        int idx = line.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx <= 0) return line;          // absent or already at start — nothing to split

        string leftPart = line[..idx].Trim();
        if (!string.IsNullOrWhiteSpace(leftPart))
            AddPairsOrFallback(leftPart, output, s_pairRegex);

        return line[idx..].TrimEnd();       // caller processes the label half normally
    }
    static int ExtractExponent10(string s)
    {
        string norm = NormalizeSig(s);
        int idx = norm.IndexOf("10^", StringComparison.Ordinal);
        if (idx < 0) return 0;

        int i = idx + 3, sign = 1;
        if (i < norm.Length && norm[i] == '+') { i++; }
        else if (i < norm.Length && norm[i] == '-') { sign = -1; i++; }

        int start = i;
        while (i < norm.Length && char.IsDigit(norm[i])) i++;
        if (i == start) return 0;

        return int.TryParse(norm.Substring(start, i - start), NumberStyles.Integer,
                            CultureInfo.InvariantCulture, out int exp) ? sign * exp : 0;
    }
    static UnitMeasurements ExtractUnitFromKey(string keyRaw, Dictionary<string, UnitMeasurements> lookup)
    {
        int l = keyRaw.IndexOf('('), r = keyRaw.IndexOf(')', l + 1);
        if (l >= 0 && r > l)
        {
            string inside = keyRaw.Substring(l + 1, r - l - 1).Trim();
            if (lookup.TryGetValue(NormalizeSig(inside), out UnitMeasurements u)) return u;
        }

        int comma = keyRaw.IndexOf(',');
        if (comma >= 0 && comma < keyRaw.Length - 1)
        {
            string after = keyRaw.Substring(comma + 1).Trim();
            int dl = after.IndexOf('(');
            if (dl >= 0) after = after.Substring(0, dl).Trim();
            if (lookup.TryGetValue(NormalizeSig(after), out UnitMeasurements u)) return u;
        }

        return UnitMeasurements.None;
    }

    static string NormalizePropertyKey(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return "unknown";

        s = s.Trim();

        s = Regex.Replace(s, @"\([^)]*\)", "");

        s = s.Replace(",", " ");
        s = s.Replace(".", " ");

        s = Regex.Replace(s, @"\s+", "_");

        s = s.Trim('_');

        return s;
    }
    static bool TryParseNumericValue(
        string valRaw, int expFromKey, UnitMeasurements unitFromKey,
        Dictionary<string, UnitMeasurements> lookup,
        out double numeric, out UnitMeasurements finalUnit)
    {
        numeric = default;
        finalUnit = unitFromKey;

        string v = valRaw.Trim();
        int pm = v.IndexOf("+-", StringComparison.Ordinal);
        if (pm >= 0) v = v.Substring(0, pm).Trim();

        int expFromVal = ExtractExponent10(v);

        // Handles "x 10^N" scientific notation
        Match sciMatch = Regex.Match(v, @"x\s*10\^([+-]?\d+)", RegexOptions.IgnoreCase);

        if (sciMatch.Success && int.TryParse(sciMatch.Groups[1].Value, out int sciExp))
            expFromVal += sciExp;


        if (finalUnit == UnitMeasurements.None)
            TryExtractSuffixUnit(ref v, lookup, out finalUnit);
        else
            TryExtractSuffixUnit(ref v, lookup, out _);

        if (!TryExtractLeadingNumber(v, out string token)) return false;
        if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out double baseVal)) return false;

        int totalExp = expFromKey + expFromVal;
        if (totalExp != 0) baseVal *= Math.Pow(10d, totalExp);

        numeric = baseVal;
        return true;

        static bool TryExtractSuffixUnit(ref string s, Dictionary<string, UnitMeasurements> lk, out UnitMeasurements unit)
        {
            unit = UnitMeasurements.None;
            string t = s.Trim();
            int last = t.LastIndexOf(' ');
            if (last < 0 || last == t.Length - 1) return false;
            string token = t.Substring(last + 1).Trim().TrimEnd(',', ';');
            if (!lk.TryGetValue(NormalizeSig(token), out unit)) return false;
            s = t.Substring(0, last).Trim();
            return true;
        }

        static bool TryExtractLeadingNumber(string s, out string token)
        {
            token = null;

            if (string.IsNullOrWhiteSpace(s))
                return false;

            Match match = Regex.Match(
                s,
                @"[-+]?(?:\d+\.?\d*|\.\d+)(?:[EeDd][-+]?\d+)?",
                RegexOptions.CultureInvariant);

            if (!match.Success)
                return false;

            token = match.Value
                .Replace('D', 'E')
                .Replace('d', 'E');

            return true;
        }
    }

    static string NormalizeSig(string s)
    {
        s = s.ToLowerInvariant();
        Span<char> tmp = s.Length <= 256 ? stackalloc char[s.Length] : new char[s.Length];
        int w = 0;
        for (int i = 0; i < s.Length; i++)
            if (!char.IsWhiteSpace(s[i])) tmp[w++] = s[i];
        return new string(tmp.Slice(0, w));
    }

    static bool TryParseCatalogLine(string rawLine, out BodyCatalog catalog)
    {
        catalog = new();

        if (string.IsNullOrWhiteSpace(rawLine))
        {
            Debug.LogError("[HorizonsParser] TryParseCatalogLine: rawLine is empty.");
            return false;
        }

        rawLine = rawLine.TrimEnd();

        string naifID = "";
        string name = "";
        string aliases = "";
        string designation = "";

        int i = 0;

        // Optional negative sign
        if (rawLine[i] == '-')
        {
            naifID += '-';
            i++;
        }

        // Parse numeric NAIF ID
        while (i < rawLine.Length && char.IsDigit(rawLine[i]))
        {
            naifID += rawLine[i];
            i++;
        }

        if (!int.TryParse(naifID, out catalog.NAIFID))
        {
            Debug.LogError($"[HorizonsParser] Bad NAIFID '{naifID}'");
            return false;
        }

        // Skip ALL whitespaces
        while (i < rawLine.Length && char.IsWhiteSpace(rawLine[i])) i++;

        // If line ends after ID, valid minimal entry
        if (i >= rawLine.Length)
        {
            catalog.Name = "";
            catalog.Aliases = "";
            catalog.Designation = "";
            return true;
        }

        static bool IsGap(string s, int idx) => idx < s.Length - 1 && char.IsWhiteSpace(s[idx]) && char.IsWhiteSpace(s[idx + 1]);

        static int SkipSpaces(string s, int idx)
        {
            while (idx < s.Length && char.IsWhiteSpace(s[idx])) idx++;
            return idx;
        }

        static string ReadToGap(string s, ref int idx)
        {
            int start = idx;

            while (idx < s.Length && !IsGap(s, idx)) idx++;

            int end = idx;

            while (end > start && char.IsWhiteSpace(s[end - 1])) end--;

            return end > start ? s[start..end] : "";
        }

        static bool LooksLikeDesignation(string t)
        {
            if (string.IsNullOrWhiteSpace(t))
                return false;

            t = t.Trim();

            if (t.Length >= 5 && char.IsDigit(t[0]) && char.IsDigit(t[1]) && char.IsDigit(t[2]) && char.IsDigit(t[3]) && char.IsLetter(t[4]))
                return true;

            if (t.Length >= 6 && char.IsDigit(t[0]) && char.IsDigit(t[1]) && char.IsDigit(t[2]) && char.IsDigit(t[3]) && t[4] == '-')
                return true;

            if (t.Length >= 2 && t[1] == '/' && "CPDXA".Contains(t[0]))
                return true;

            return false;
        }

        // Read name field
        name = ReadToGap(rawLine, ref i);

        i = SkipSpaces(rawLine, i);
        if (i >= rawLine.Length)
        {
            catalog.Name = name;
            catalog.Aliases = "";
            catalog.Designation = "";
            return true;
        }

        string field1 = ReadToGap(rawLine, ref i);
        i = SkipSpaces(rawLine, i);
        string field2 = i < rawLine.Length ? rawLine[i..].Trim() : "";

        if (!string.IsNullOrEmpty(field1))
        {
            if (LooksLikeDesignation(field1))
            {
                designation = field1;
                aliases = field2;
            }
            else
            {
                aliases = field1;
                if (!string.IsNullOrEmpty(field2)) aliases = $"{aliases} {field2}";
            }
        }

        catalog.Name = name;
        catalog.Aliases = aliases;
        catalog.Designation = designation;
        return true;
    }
}