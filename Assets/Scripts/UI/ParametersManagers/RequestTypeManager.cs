using TMPro;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public class RequestTypeManager : MonoBehaviour, IDefaultable
{
    [SerializeField] TMP_Dropdown _requestTypeDropdown;
    int _vecTableValue;

    [SerializeField] TimeFormatManager timeFormatManager;
    [SerializeField] StartTimeManager startTimeManager;
    [SerializeField] StopTimeManager stopTimeManager;
    [SerializeField] StepSizeManager stepSizeManager;
    [SerializeField] TListManager tListManager;


    public void OnDropdownValueChanged(int value)
    {
        RequestTypeSimple requestTypeSimple = (RequestTypeSimple)value;
        switch (requestTypeSimple)
        {
            case RequestTypeSimple.Position:
                timeFormatManager.GetParameterContainer().SetActive(true);
                timeFormatManager.ChangeParameters();

                break;
            case RequestTypeSimple.Velocity:
                timeFormatManager.GetParameterContainer().SetActive(true);
                timeFormatManager.ChangeParameters();

                break;
            case RequestTypeSimple.PosAndVel:
                timeFormatManager.GetParameterContainer().SetActive(true);
                timeFormatManager.ChangeParameters();

                break;
            case RequestTypeSimple.PhysicalTraits:
                timeFormatManager.GetParameterContainer().SetActive(false);
                startTimeManager.GetParameterContainer().SetActive(false);
                stopTimeManager.GetParameterContainer().SetActive(false);
                stepSizeManager.GetParameterContainer().SetActive(false);
                tListManager.GetParameterContainer().SetActive(false);
                break;
        }
    }
    public void ApplyDefault()
    {
        SimplifiedMode();
    }

    public void FullMode()
    {
        List<string> requestTypeList = Enum.GetNames(typeof(RequestTypeFull)).ToList();
        _requestTypeDropdown.ClearOptions();
        _requestTypeDropdown.AddOptions(requestTypeList);
        _requestTypeDropdown.captionText.fontSize = 16;
        _requestTypeDropdown.captionText.fontStyle = FontStyles.Bold;
        _requestTypeDropdown.itemText.fontSize = 16;
        _requestTypeDropdown.itemText.fontStyle = FontStyles.Bold;
    }

    public void SimplifiedMode()
    {
        List<string> requestTypeList = Enum.GetNames(typeof(RequestTypeSimple)).ToList();
        _requestTypeDropdown.ClearOptions();
        _requestTypeDropdown.AddOptions(requestTypeList);
        _requestTypeDropdown.captionText.fontSize = 16;
        _requestTypeDropdown.captionText.fontStyle = FontStyles.Bold;
        _requestTypeDropdown.itemText.fontSize = 16;
        _requestTypeDropdown.itemText.fontStyle = FontStyles.Bold;
    }

    public int GetVectorTable()
    {
        RequestTypeSimple requestTypeSimple = (RequestTypeSimple)_requestTypeDropdown.value;
        return requestTypeSimple switch
        {
            RequestTypeSimple.Position => 1,
            RequestTypeSimple.Velocity => 5,
            RequestTypeSimple.PosAndVel => 2,
            _ => 0,
        };
    }

    public int GetVecTableValue => _vecTableValue;
    public TMP_Dropdown GetDropdown() => _requestTypeDropdown;
}

public enum RequestTypeFull
{
    Ephemeris,
    ObjectData,
    Both,
    Database
}

public enum RequestTypeSimple
{
    Position,
    Velocity,
    PosAndVel,
    PhysicalTraits,
    Rotation
}


