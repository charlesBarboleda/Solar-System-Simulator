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
    public int RequestTypeValue => _requestTypeDropdown.value;
    // Output Format
    [SerializeField] TMP_Dropdown _outputFormatDropdown;
    public int OutputFormatValue => _outputFormatDropdown.value;
    // Ephemeris Type
    [SerializeField] TMP_Dropdown _ephemerisTypeDropdown;
    public int EphemerisTypeDropdown => _ephemerisTypeDropdown.value;
    // Reference Plane
    [SerializeField] TMP_Dropdown _referencePlaneDropdown;
    public int ReferencePlaneDropdown => _referencePlaneDropdown.value;
    // Coordinate Type
    [SerializeField] TMP_Dropdown _coordinateTypeDropdown;
    public int CoordinateTypeDropdown => _coordinateTypeDropdown.value;

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
            _coordinateTypeDropdown.itemText.fontSize = 16;
            _coordinateTypeDropdown.itemText.fontStyle = FontStyles.Bold;
        }
    }

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
            _referencePlaneDropdown.itemText.fontSize = 16;
            _referencePlaneDropdown.itemText.fontStyle = FontStyles.Bold;
        }
    }

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
            _ephemerisTypeDropdown.itemText.fontSize = 16;
            _ephemerisTypeDropdown.itemText.fontStyle = FontStyles.Bold;
        }
    }

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
            List<string> responseTypeList = Enum.GetNames(typeof(ResponseType)).ToList();
            _requestTypeDropdown.ClearOptions();
            _requestTypeDropdown.AddOptions(responseTypeList);
            _requestTypeDropdown.itemText.fontSize = 16;
            _requestTypeDropdown.itemText.fontStyle = FontStyles.Bold;
        }
    }

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
            _outputFormatDropdown.itemText.fontSize = 16;
            _outputFormatDropdown.itemText.fontStyle = FontStyles.Bold;
        }
    }


}
