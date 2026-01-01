using UnityEngine;

public enum Format
{
    text,
    json
}

public enum EphemerisType
{
    OBSERVER,
    VECTORS,
    ELEMENTS,
    SPK,
    APPROACH
}

public enum ReferencePlane
{
    ECLIPTIC,
    FRAME,
    BODYEQUATOR
}

public enum StepSizeUnit
{
    days,
    hours,
    minutes,
    years,
    months
}

public enum OutputUnits
{
    KM_S,
    AU_D,
    KM_D,

}

public enum ReferenceSystem
{
    ICRF,
    B1950
}

public struct HorizonsSearchSettings
{
    public const string HORIZONS_BASE_URL = "https://ssd.jpl.nasa.gov/api/horizons.api?";
    public string Result;
    Format _formatType;
    public readonly string FormatType
    {
        get
        {
            return "format=" + $"'{_formatType}'";
        }
    }

    [Tooltip("The body to display ephemeris for. Must be a valid Horizons ID.")]
    int _command; // ID of the object
    public string Command
    {
        get
        {
            return "COMMAND=" + $"'{Command}'";
        }
    }

    bool _objData;
    public readonly string ObjData
    {
        get
        {
            return "OBJ_DATA=" + $"'{(_objData ? "YES" : "NO")}'";
        }
    }
    bool _makeEphemeris;
    public readonly string MakeEphemeris
    {
        get
        {
            return "MAKE_EPHEM=" + $"'{(_makeEphemeris ? "YES" : "NO")}'";
        }
    }

    [Header("Ephemeris specific settings")]
    EphemerisType _ephemerisType;
    public readonly string EphemerisType
    {
        get
        {
            return "EPHEM_TYPE=" + $"'{_ephemerisType}'";
        }
    }

    string _coordinateCenter; // ID of the center body
    public readonly string CoordinateCenter
    {
        get
        {
            return "CENTER=" + $"'{_coordinateCenter}'";
        }
    }
    string _startTime; // Format: YYYY-MM-DD HH:MM:SS
    public readonly string StartTime
    {
        get
        {
            return "START_TIME=" + $"'{_startTime}'";
        }
    }
    string _stopTime;  // Format: YYYY-MM-DD HH:MM:SS
    public string StopTime
    {
        get
        {
            return "STOP_TIME=" + $"'{_stopTime}'";
        }
    }
    StepSizeUnit _stepSizeUnit;
    int _stepSizeValue;
    public readonly string StepSize
    {
        get
        {
            return _stepSizeUnit switch
            {
                StepSizeUnit.days => "STEP_SIZE=" + $"'{_stepSizeValue + "d"}'",
                StepSizeUnit.hours => "STEP_SIZE=" + $"'{_stepSizeValue + "h"}'",
                StepSizeUnit.minutes => "STEP_SIZE=" + $"'{_stepSizeValue + "m"}'",
                StepSizeUnit.years => "STEP_SIZE=" + $"'{_stepSizeValue + "y"}'",
                StepSizeUnit.months => "STEP_SIZE=" + $"'{_stepSizeValue + "mo"}'",
                _ => "STEP_SIZE='60min'",
            };
        }
    }
    ReferencePlane _referencePlane;
    public readonly string ReferencePlane
    {
        get
        {
            return "REF_PLANE=" + $"'{_referencePlane}'";
        }
    }

    OutputUnits _outputUnits;
    public readonly string OutputUnit
    {
        get
        {
            return _outputUnits switch
            {
                OutputUnits.KM_S => "OUT_UNITS='KM-S'",
                OutputUnits.AU_D => "OUT_UNITS='AU-D'",
                OutputUnits.KM_D => "OUT_UNITS='KM-D'",
                _ => "OUT_UNITS='KM-S'",

            };
        }
    }

    int _vecTable;
    public readonly string VecTable
    {
        get
        {
            return "VEC_TABLE=" + $"'{_vecTable}'";
        }
    }

    ReferenceSystem _refSystem;
    public readonly string ReferenceSystem
    {
        get
        {
            return "REF_SYSTEM=" + $"'{_refSystem}'";
        }
    }


    public string EmailAddress;

    public HorizonsSearchSettings(
        int command,
        string coordinateCenter,
        string startTime,
        string stopTime,
        int stepSizeValue,
        Format formatType = Format.json,
        bool objData = true,
        bool makeEphemeris = true,
        StepSizeUnit stepSizeUnit = global::StepSizeUnit.days,
        EphemerisType ephemerisType = global::EphemerisType.VECTORS,
        ReferencePlane refPlane = global::ReferencePlane.ECLIPTIC,
        ReferenceSystem referenceSystem = global::ReferenceSystem.ICRF,
        OutputUnits outputUnits = OutputUnits.KM_S,
        int vecTable = 2,
        string email = null,
        string result = null
    )
    {
        _command = command;
        _coordinateCenter = coordinateCenter;
        _startTime = startTime;
        _stopTime = stopTime;
        _stepSizeUnit = stepSizeUnit;
        _stepSizeValue = stepSizeValue;
        _formatType = formatType;
        _objData = objData;
        _makeEphemeris = makeEphemeris;
        _ephemerisType = ephemerisType;
        _referencePlane = refPlane;
        _refSystem = referenceSystem;
        _outputUnits = outputUnits;
        _vecTable = vecTable;
        EmailAddress = email;
        Result = result;
    }

    public readonly string BuildQuery(HorizonsSearchSettings _settings)
    {
        string result = HORIZONS_BASE_URL;
        result += _settings.FormatType + "&";
        result += _settings.Command + "&";
        result += _settings.ObjData + "&";
        result += _settings.MakeEphemeris + "&";
        result += _settings.EphemerisType + "&";
        result += _settings.CoordinateCenter + "&";
        result += _settings.ReferencePlane + "&";
        result += _settings.StartTime + "&";
        result += _settings.StopTime + "&";
        result += _settings.StepSize + "&";
        result += _settings.ReferenceSystem + "&";
        result += _settings.OutputUnit + "&";
        result += _settings.VecTable + "&";

        return result;
    }


}



