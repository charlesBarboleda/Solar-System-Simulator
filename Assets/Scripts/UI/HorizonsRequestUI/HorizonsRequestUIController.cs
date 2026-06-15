using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class HorizonsRequestUIController : MonoBehaviour
{
    // UI Element References
    // Request Type
    [SerializeField] TMP_Dropdown _requestTypeDropdown;
    // Output Format
    [SerializeField] TMP_Dropdown _outputFormatDropdown;
    [SerializeField] TMP_Dropdown _ephemerisTypeDropdown;
    // Reference Plane
    [SerializeField] TMP_Dropdown _referencePlaneDropdown;
    // Coordinate Type
    [SerializeField] TMP_Dropdown _coordinateTypeDropdown;
    // Step Size Time Unit
    [SerializeField] TMP_Dropdown _stepSizeUnitDropdown;
    // Time Digits
    [SerializeField] TMP_Dropdown _timeDigitsDropdown;
    // Time Type
    [SerializeField] TMP_Dropdown _timeTypeDropdown;
    // TList Type
    [SerializeField] TMP_Dropdown _tListTypeDropdown;


    [ContextMenu("Init All")]
    public void InitAllUIElements()
    {
        InitCoordTypeDropdown();
        InitEphemerisTypeDropdown();
        InitOutputFormatDropdown();
        InitReferencePlaneDropdown();
        InitRequestTypeDropdown();
        InitStepSizeUnitDropdown();
        InitTimeDigitsDropdown();
        InitTimeTypeDropdown();
        InitTListTypeDropdown();
    }

    [ContextMenu("Init TListType dropdown")]
    public void InitTListTypeDropdown()
    {
        if (_tListTypeDropdown == null)
        {
            Debug.LogError("UI element references missing. Check the inspector.");
            return;
        }
        else
        {
            List<string> _tListTypeDropdownList = Enum.GetNames(typeof(TListType)).ToList();
            _tListTypeDropdown.ClearOptions();
            _tListTypeDropdown.AddOptions(_tListTypeDropdownList);
            _tListTypeDropdown.captionText.fontSize = 16;
            _tListTypeDropdown.captionText.fontStyle = FontStyles.Bold;
            _tListTypeDropdown.itemText.fontSize = 16;
            _tListTypeDropdown.itemText.fontStyle = FontStyles.Bold;
        }
    }
    public TListType GetTListTypeDropdownValue() => (TListType)_tListTypeDropdown.value;

    [ContextMenu("Init TimeType dropdown")]
    public void InitTimeTypeDropdown()
    {
        if (_timeTypeDropdown == null)
        {
            Debug.LogError("UI element references missing. Check the inspector.");
            return;
        }
        else
        {
            List<string> _timeTypeDropdownList = Enum.GetNames(typeof(TimeType)).ToList();
            _timeTypeDropdown.ClearOptions();
            _timeTypeDropdown.AddOptions(_timeTypeDropdownList);
            _timeTypeDropdown.captionText.fontSize = 16;
            _timeTypeDropdown.captionText.fontStyle = FontStyles.Bold;
            _timeTypeDropdown.itemText.fontSize = 16;
            _timeTypeDropdown.itemText.fontStyle = FontStyles.Bold;
        }
    }
    public TimeType GetTimeTypeValue() => (TimeType)_timeTypeDropdown.value;

    [ContextMenu("Init TimeDigits dropdown")]
    public void InitTimeDigitsDropdown()
    {
        if (_timeDigitsDropdown == null)
        {
            Debug.LogError("UI element references missing. Check the inspector.");
            return;
        }
        else
        {
            List<string> _timeDigitsDropdownList = Enum.GetNames(typeof(TimeDigits)).ToList();
            _timeDigitsDropdown.ClearOptions();
            _timeDigitsDropdown.AddOptions(_timeDigitsDropdownList);
            _timeDigitsDropdown.captionText.fontSize = 16;
            _timeDigitsDropdown.captionText.fontStyle = FontStyles.Bold;
            _timeDigitsDropdown.itemText.fontSize = 16;
            _timeDigitsDropdown.itemText.fontStyle = FontStyles.Bold;
        }
    }
    public TimeDigits GetTimeDigitsValue() => (TimeDigits)_timeDigitsDropdown.value;

    [ContextMenu("Init Step Size Unit Dropdown")]
    public void InitStepSizeUnitDropdown()
    {
        if (_stepSizeUnitDropdown == null)
        {
            Debug.LogError("UI element references missing. Check the inspector.");
            return;
        }
        else
        {
            List<string> _stepSizeUnitDropdownList = Enum.GetNames(typeof(StepSizeUnit)).ToList();
            _stepSizeUnitDropdown.ClearOptions();
            _stepSizeUnitDropdown.AddOptions(_stepSizeUnitDropdownList);
            _stepSizeUnitDropdown.captionText.fontSize = 16;
            _stepSizeUnitDropdown.captionText.fontStyle = FontStyles.Bold;
            _stepSizeUnitDropdown.itemText.fontSize = 16;
            _stepSizeUnitDropdown.itemText.fontStyle = FontStyles.Bold;
        }
    }
    public StepSizeUnit GetStepSizeUnitValue() => (StepSizeUnit)_stepSizeUnitDropdown.value;

    [ContextMenu("Init CoordType Dropdown")]
    public void InitCoordTypeDropdown()
    {
        if (_coordinateTypeDropdown == null)
        {
            Debug.LogError("UI element references missing. Check the inspector.");
            return;
        }
        else
        {
            List<string> coordTypeList = Enum.GetNames(typeof(CoordinateType)).ToList();
            _coordinateTypeDropdown.ClearOptions();
            _coordinateTypeDropdown.AddOptions(coordTypeList);
            _coordinateTypeDropdown.captionText.fontSize = 16;
            _coordinateTypeDropdown.captionText.fontStyle = FontStyles.Bold;
            _coordinateTypeDropdown.itemText.fontSize = 16;
            _coordinateTypeDropdown.itemText.fontStyle = FontStyles.Bold;
        }
    }
    public CoordinateType GetCoordinateTypeValue() => (CoordinateType)_coordinateTypeDropdown.value;

    [ContextMenu("Init RefPlane Dropdown")]
    public void InitReferencePlaneDropdown()
    {
        if (_referencePlaneDropdown == null)
        {
            Debug.LogError("UI element references missing. Check the inspector.");
            return;
        }
        else
        {
            List<string> referencePlaneList = Enum.GetNames(typeof(ReferencePlane)).ToList();
            _referencePlaneDropdown.ClearOptions();
            _referencePlaneDropdown.AddOptions(referencePlaneList);
            _referencePlaneDropdown.captionText.fontSize = 16;
            _referencePlaneDropdown.captionText.fontStyle = FontStyles.Bold;
            _referencePlaneDropdown.itemText.fontSize = 16;
            _referencePlaneDropdown.itemText.fontStyle = FontStyles.Bold;
        }
    }
    public ReferencePlane GetReferencePlaneValue() => (ReferencePlane)_referencePlaneDropdown.value;

    [ContextMenu("Init Ephemeris Type Dropdown")]
    public void InitEphemerisTypeDropdown()
    {
        if (_ephemerisTypeDropdown == null)
        {
            Debug.LogError("UI element references missing. Check the inspector.");
            return;
        }
        else
        {
            List<string> ephemerisTypeList = Enum.GetNames(typeof(EphemerisType)).ToList();
            _ephemerisTypeDropdown.ClearOptions();
            _ephemerisTypeDropdown.AddOptions(ephemerisTypeList);
            _ephemerisTypeDropdown.captionText.fontSize = 16;
            _ephemerisTypeDropdown.captionText.fontStyle = FontStyles.Bold;
            _ephemerisTypeDropdown.itemText.fontSize = 16;
            _ephemerisTypeDropdown.itemText.fontStyle = FontStyles.Bold;
        }
    }
    public EphemerisType GetEphemerisTypeValue() => (EphemerisType)_ephemerisTypeDropdown.value;

    [ContextMenu("Init Request Type Dropdown")]
    public void InitRequestTypeDropdown()
    {
        if (_requestTypeDropdown == null)
        {
            Debug.LogError("UI element references missing. Check the inspector.");
            return;
        }
        else
        {
            List<string> requestTypeList = Enum.GetNames(typeof(RequestTypeFull)).ToList();
            _requestTypeDropdown.ClearOptions();
            _requestTypeDropdown.AddOptions(requestTypeList);
            _requestTypeDropdown.captionText.fontSize = 16;
            _requestTypeDropdown.captionText.fontStyle = FontStyles.Bold;
            _requestTypeDropdown.itemText.fontSize = 16;
            _requestTypeDropdown.itemText.fontStyle = FontStyles.Bold;
        }
    }
    public RequestTypeFull GetResponseTypeValue() => (RequestTypeFull)_requestTypeDropdown.value;

    [ContextMenu("Init Output Format Dropdown")]
    public void InitOutputFormatDropdown()
    {
        if (_outputFormatDropdown == null)
        {
            Debug.LogError("UI element references missing. Check the inspector.");
            return;
        }
        else
        {
            List<string> outputFormatList = Enum.GetNames(typeof(HorizonsFormat)).ToList();
            _outputFormatDropdown.ClearOptions();
            _outputFormatDropdown.AddOptions(outputFormatList);
            _outputFormatDropdown.captionText.fontSize = 16;
            _outputFormatDropdown.captionText.fontStyle = FontStyles.Bold;
            _outputFormatDropdown.itemText.fontSize = 16;
            _outputFormatDropdown.itemText.fontStyle = FontStyles.Bold;
        }
    }
    public HorizonsFormat GetHorizonsFormatValue() => (HorizonsFormat)_outputFormatDropdown.value;

}
