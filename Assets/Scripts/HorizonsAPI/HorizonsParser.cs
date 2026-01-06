using Unity.Mathematics;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;
using System.Globalization;


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


    public static string[] FormatResponse(HorizonsResponse response, bool removeWhiteSpace = false, bool lowercase = false)
    {
        string[] splitResults = response.result.Replace(" ", "").Split('\n');
        List<string> formattedBodyData = new();
        List<EphemerisSample> formattedVectors = new();

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
                formattedBodyData.Add(row ?? string.Empty);
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
                formattedBodyData.Add(row);
            }
            else
            {
                formattedBodyData.Add(row[..splitIndex]);
                formattedBodyData.Add(row[splitIndex..]);
            }

        }

        // Object Ephemeris Formatting (TODO: Most likely scrap everything here and redo it)
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

            EphemerisSample ephemeris = ParseVectors(splitResults[dateIndex], splitResults[posIndex], splitResults[velIndex]);

            formattedVectors.Add(ephemeris);
        }

        for (int i = bodyDataEndIndex; i < splitResults.Length; i++)
        {
            formattedBodyData.Add(splitResults[i]);
        }

        if (removeWhiteSpace)
            for (int i = 0; i < splitResults.Length; i++)
                splitResults[i] = splitResults[i].Replace(" ", "");
        if (lowercase)
            for (int i = 0; i < splitResults.Length; i++)
                splitResults[i] = splitResults[i].ToLower();

        foreach (var e in formattedVectors)
        {
            Debug.Log($"Date: {e.Date}");
            Debug.Log($"Position: {e.Position}");
            Debug.Log($"Velocity: {e.Velocity}");
        }

        return formattedBodyData.ToArray();
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
            _ => "Invalid Parsable Data",
        };
    }

    // Velocity Vector Format Example: "XV=-3.271541395225262E+07YV=1.428771211383301E+08ZV=1.914442883169651E+04"
    // Position Vector Format Example: "X=-3.271541395225262E+07Y=1.428771211383301E+08Z=1.914442883169651E+04"
    static EphemerisSample ParseVectors(string date, string positions, string velocities, int maxDigitValue = 21)
    {
        if (!TryParseTriplet(positions, out double3 pos))
            throw new FormatException($"Failed to parse positions: '{positions}'");

        if (!TryParseTriplet(velocities, out double3 vel, treatAsVelocity: true))
            throw new FormatException($"Failed to parse velocities: '{velocities}'");

        return new EphemerisSample(date: date, position: pos, velocity: vel);
    }

    static bool TryParseTriplet(string s, out double3 v, bool treatAsVelocity = false)
    {
        v = default;
        if (string.IsNullOrWhiteSpace(s)) return false;

        if (treatAsVelocity)
        {
            if (TryParseLabeledTriplet(s, "VX=", "VY=", "VZ=", out v)) return true;
            if (TryParseLabeledTriplet(s, "XV=", "YV=", "ZV=", out v)) return true;
        }

        return TryParseLabeledTriplet(s, "X=", "Y=", "Z=", out v);
    }

    static bool TryParseLabeledTriplet(string s, string xLabel, string yLabel, string zLabel, out double3 v)
    {
        v = default;

        int xIdx = s.IndexOf(xLabel, StringComparison.OrdinalIgnoreCase);
        int yIdx = s.IndexOf(yLabel, StringComparison.OrdinalIgnoreCase);
        int zIdx = s.IndexOf(zLabel, StringComparison.OrdinalIgnoreCase);

        if (xIdx < 0 || yIdx < 0 || zIdx < 0) return false;
        if (!(xIdx < yIdx && yIdx < zIdx)) return false;

        int xStart = xIdx + xLabel.Length;
        int yStart = yIdx + yLabel.Length;
        int zStart = zIdx + zLabel.Length;

        string xText = s.Substring(xStart, yIdx - xStart).Trim();
        string yText = s.Substring(yStart, zIdx - yStart).Trim();
        string zText = s.Substring(zStart).Trim();

        const NumberStyles style = NumberStyles.Float | NumberStyles.AllowLeadingSign;
        var culture = CultureInfo.InvariantCulture;

        if (!double.TryParse(xText, style, culture, out double x)) return false;
        if (!double.TryParse(yText, style, culture, out double y)) return false;
        if (!double.TryParse(zText, style, culture, out double z)) return false;

        v = new double3(x, y, z);
        return true;
    }
}
