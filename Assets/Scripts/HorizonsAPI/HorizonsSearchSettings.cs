using UnityEngine;

public struct HorizonsSearchSettings
{
    public const string HORIZONS_BASE_URL = "https://ssd.jpl.nasa.gov/api/horizons.api?";
    public readonly string Result;
    public readonly ResponseType ResponseType
    {
        get
        {
            if (!_objData && _makeEphemeris) return ResponseType.Ephemeris;
            if (_objData && !_makeEphemeris) return ResponseType.ObjectData;
            if (_objData && _makeEphemeris) return ResponseType.Both;
            return ResponseType.ObjectData;
        }
    }

    readonly HorizonsFormat _horizonFormatType;
    public readonly string HorizonFormatType
    {
        get
        {
            return "format=" + $"{_horizonFormatType}";
        }
    }

    public BodySearchType BodySearchType;
    public string BodyName;
    public int BodyID;
    readonly int _commandID;
    readonly int _commandNameToID
    {
        get
        {
            return 2;
        }
    }

    public string TestCommandID;
    public readonly string Command
    {
        get
        {
            return BodySearchType switch
            {
                BodySearchType.NAIFID => "COMMAND=" + $"'{_commandID}'",
                BodySearchType.Name => "COMMAND=" + $"'{TestCommandID}'",
                _ => "COMMAND=" + $"'{_commandID}'"
            };
        }
    }

    readonly bool _objData;
    public readonly string ObjData
    {
        get
        {
            return "OBJ_DATA=" + $"'{(_objData ? "YES" : "NO")}'";
        }
    }
    readonly bool _makeEphemeris;
    public readonly string MakeEphemeris
    {
        get
        {
            return "MAKE_EPHEM=" + $"'{(_makeEphemeris ? "YES" : "NO")}'";
        }
    }

    [Header("Ephemeris specific settings")]
    readonly EphemerisType _ephemerisType;
    public readonly string EphemerisType
    {
        get
        {
            return "EPHEM_TYPE=" + $"'{_ephemerisType}'";
        }
    }

    readonly string _coordinateCenter; // ID of the center body
    public readonly string CoordinateCenter
    {
        get
        {
            return "CENTER=" + $"'{_coordinateCenter}'";
        }
    }

    readonly string _startTime; // HorizonFormat: YYYY-MM-DD HH:MM:SS
    public readonly string StartTime
    {
        get
        {
            return "START_TIME=" + $"'{_startTime}'";
        }
    }

    readonly string _stopTime;  // HorizonFormat: YYYY-MM-DD HH:MM:SS
    public readonly string StopTime
    {
        get
        {
            return "STOP_TIME=" + $"'{_stopTime}'";
        }
    }

    readonly StepSizeUnit _stepSizeUnit;
    readonly int _stepSizeValue;
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

    readonly ReferencePlane _referencePlane;
    public readonly string ReferencePlane
    {
        get
        {
            return "REF_PLANE=" + $"'{_referencePlane}'";
        }
    }

    readonly OutputUnits _outputUnits;
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

    readonly int _vecTable;
    public readonly string VecTable
    {
        get
        {
            return "VEC_TABLE=" + $"'{_vecTable}'";
        }
    }

    readonly ReferenceSystem _refSystem;
    public readonly string ReferenceSystem
    {
        get
        {
            return "REF_SYSTEM=" + $"'{_refSystem}'";
        }
    }


    public readonly string EmailAddress;

    public HorizonsSearchSettings(
        string testCommandID,
        int commandID,
        string bodyName,
        int bodyID,
        string coordinateCenter,
        string startTime,
        string stopTime,
        int stepSizeValue,
        BodySearchType bodySearchType,
        HorizonsFormat horizonFormatType = HorizonsFormat.json,
        bool objData = true,
        bool makeEphemeris = true,
        StepSizeUnit stepSizeUnit = StepSizeUnit.days,
        EphemerisType ephemerisType = global::EphemerisType.VECTORS,
        ReferencePlane refPlane = global::ReferencePlane.ECLIPTIC,
        ReferenceSystem referenceSystem = global::ReferenceSystem.ICRF,
        OutputUnits outputUnits = OutputUnits.KM_S,
        int vecTable = 2,
        string email = null,
        string result = null
    )
    {
        TestCommandID = testCommandID;
        _commandID = commandID;
        BodySearchType = bodySearchType;
        BodyName = bodyName;
        BodyID = bodyID;
        _coordinateCenter = coordinateCenter;
        _startTime = startTime;
        _stopTime = stopTime;
        _stepSizeUnit = stepSizeUnit;
        _stepSizeValue = stepSizeValue;
        _horizonFormatType = horizonFormatType;
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
        ResponseType type = _settings.ResponseType;
        string url = HORIZONS_BASE_URL;

        switch (type)
        {
            case ResponseType.Ephemeris:
                url += _settings.HorizonFormatType + "&";
                url += _settings.Command + "&";
                url += _settings.ObjData + "&";
                url += _settings.MakeEphemeris + "&";
                url += _settings.EphemerisType + "&";
                url += _settings.CoordinateCenter + "&";
                url += _settings.ReferencePlane + "&";
                url += _settings.StartTime + "&";
                url += _settings.StopTime + "&";
                url += _settings.StepSize + "&";
                url += _settings.ReferenceSystem + "&";
                url += _settings.OutputUnit + "&";
                url += _settings.VecTable;
                return url;
            case ResponseType.ObjectData:
                url += _settings.HorizonFormatType + "&";
                url += _settings.Command + "&";
                url += _settings.ObjData;
                return url;
            case ResponseType.Both:
                url += _settings.HorizonFormatType + "&";
                url += _settings.Command + "&";
                url += _settings.ObjData + "&";
                url += _settings.MakeEphemeris + "&";
                url += _settings.EphemerisType + "&";
                url += _settings.CoordinateCenter + "&";
                url += _settings.ReferencePlane + "&";
                url += _settings.StartTime + "&";
                url += _settings.StopTime + "&";
                url += _settings.StepSize + "&";
                url += _settings.ReferenceSystem + "&";
                url += _settings.OutputUnit + "&";
                url += _settings.VecTable;
                return url;
        }

        return url;
    }

}

public enum CoordinateType
{
    GEODETIC,
    CYLINDRICAL
}

public enum HorizonsFormat
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

public enum ResponseType
{
    Ephemeris,
    ObjectData,
    Both,
    Database
}

public enum BodySearchType
{
    NAIFID,
    Name
}




