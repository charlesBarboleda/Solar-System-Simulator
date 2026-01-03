using Unity.Mathematics;
using System.Collections.Generic;
using System;
using UnityEngine;


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
        string[] splitResults = response.result.Split('\n');

        if (removeWhiteSpace)
            for (int i = 0; i < splitResults.Length; i++)
                splitResults[i] = splitResults[i].Replace(" ", "");
        if (lowercase)
            for (int i = 0; i < splitResults.Length; i++)
                splitResults[i] = splitResults[i].ToLower();

        return splitResults;
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

}
