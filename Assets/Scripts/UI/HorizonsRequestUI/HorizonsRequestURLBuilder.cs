using System.Collections.Generic;
using UnityEngine;

public class HorizonsRequestURLBuilder
{
    const string HORIZONS_BASE_URL = "https://ssd.jpl.nasa.gov/api/horizons.api?";

    RequestTypeSimple _requestTypeSimple;
    RequestTypeFull _requestTypeFull;
    List<IAPIParameterManager> _parameterManagers;

    RequestTypeManager _requestTypeManager;
    MainBodyNAIFManager _mainBodyNAIFIDManager;
    TimeFormatManager _timeFormatManager;
    StartTimeManager _startTimeManager;
    StopTimeManager _stopTimeManager;
    StepSizeManager _stepSizeManager;
    TListManager _tListManager;

    public HorizonsRequestURLBuilder(RequestTypeSimple requestTypeSimple, RequestTypeManager requestTypeManager, TimeFormatManager timeFormatManager, List<IAPIParameterManager> parameterManagers)
    {
        _requestTypeSimple = requestTypeSimple;
        _requestTypeManager = requestTypeManager;
        _timeFormatManager = timeFormatManager;
        _parameterManagers = parameterManagers;
        ValidateParameters();
    }

    public HorizonsRequestURLBuilder(RequestTypeFull requestTypeFull, RequestTypeManager requestTypeManager, TimeFormatManager timeFormatManager, List<IAPIParameterManager> parameterManagers)
    {
        _requestTypeFull = requestTypeFull;
        _requestTypeManager = requestTypeManager;
        _timeFormatManager = timeFormatManager;
        _parameterManagers = parameterManagers;
        ValidateParameters();
    }

    bool ValidateParameters()
    {
        TimeFormat timeFormat = _timeFormatManager.GetTimeFormat();

        switch (_requestTypeSimple)
        {
            case RequestTypeSimple.Position:
            case RequestTypeSimple.Velocity:
            case RequestTypeSimple.PosAndVel:
                AssignEphemerisParameterManagers(timeFormat);
                break;
            case RequestTypeSimple.PhysicalTraits:
                AssignPhysicalTraitsParameterManagers();
                break;
        }

        return true;
    }

    // Shared by Position, Velocity, and PosAndVel
    void AssignEphemerisParameterManagers(TimeFormat timeFormat)
    {
        foreach (var manager in _parameterManagers)
        {
            if (manager is MainBodyNAIFManager mainBody && ValidateManager(mainBody, "mainBodyNAIFID"))
                _mainBodyNAIFIDManager = mainBody;

            if (timeFormat == TimeFormat.Range)
            {
                if (manager is StartTimeManager start && ValidateManager(start, "startTime"))
                    _startTimeManager = start;

                if (manager is StopTimeManager stop && ValidateManager(stop, "stopTime"))
                    _stopTimeManager = stop;

                if (manager is StepSizeManager step && ValidateManager(step, "stepSize"))
                    _stepSizeManager = step;
            }
            else if (timeFormat == TimeFormat.Specific)
            {
                if (manager is TListManager tList && ValidateManager(tList, "tList"))
                    _tListManager = tList;
            }
        }
    }

    void AssignPhysicalTraitsParameterManagers()
    {
        foreach (var manager in _parameterManagers)
        {
            if (manager is MainBodyNAIFManager mainBody && ValidateManager(mainBody, "mainBodyNAIFID"))
            {
                _mainBodyNAIFIDManager = mainBody;
                break;
            }
        }
    }

    bool ValidateManager(IAPIParameterManager manager, string paramName)
    {
        if (!manager.TryGetURL(out _))
        {
            Debug.LogError($"'{paramName}' input is invalid.");
            return false;
        }
        return true;
    }

    public void BuildSimpleURL(out string URL)
    {
        URL = HORIZONS_BASE_URL + "format=json&";

        string mainBodyURL = _mainBodyNAIFIDManager.TryGetURL(out string mainBodyURLResult) ? mainBodyURLResult : string.Empty;
        TimeFormat timeFormat = _timeFormatManager.GetTimeFormat();

        switch (_requestTypeSimple)
        {
            case RequestTypeSimple.Position:
            case RequestTypeSimple.Velocity:
            case RequestTypeSimple.PosAndVel:
                if (!TryBuildEphemerisURL(mainBodyURL, timeFormat, out string ephemerisURL))
                {
                    URL = string.Empty;
                    return;
                }
                URL += ephemerisURL;
                break;
            case RequestTypeSimple.PhysicalTraits:
                URL += $"{mainBodyURL}&OBJ_DATA='YES'&MAKE_EPHEM='NO'&EPHEM_TYPE='OBSERVER'";
                break;
        }
    }

    bool TryBuildEphemerisURL(string mainBodyURL, TimeFormat timeFormat, out string url)
    {
        const string commonParams = "OBJ_DATA='NO'&MAKE_EPHEM='YES'&EPHEM_TYPE='VECTORS'&CENTER='500@10'&REF_PLANE='ECLIPTIC'&OUT_UNITS='AU-D'";
        string vecTable = $"VEC_TABLE='{_requestTypeManager.GetVectorTable()}'";

        if (timeFormat == TimeFormat.Range)
        {
            if (!_startTimeManager.TryGetURL(out string startTimeURL) ||
                !_stopTimeManager.TryGetURL(out string stopTimeURL) ||
                !_stepSizeManager.TryGetURL(out string stepSizeURL))
            {
                Debug.LogError("Invalid time range parameters.");
                url = string.Empty;
                return false;
            }
            url = $"{mainBodyURL}&{commonParams}&{startTimeURL}&{stopTimeURL}&{stepSizeURL}&{vecTable}";
            return true;
        }
        else if (timeFormat == TimeFormat.Specific)
        {
            if (!_tListManager.TryGetURL(out string tListURL))
            {
                Debug.LogError("Invalid time specific parameters.");
                url = string.Empty;
                return false;
            }
            url = $"{mainBodyURL}&{commonParams}&{tListURL}&{vecTable}";
            return true;
        }

        url = string.Empty;
        return false;
    }

    public string BuildFullURL(out string URL)
    {
        URL = HORIZONS_BASE_URL;
        // Implement later
        return URL;
    }
}