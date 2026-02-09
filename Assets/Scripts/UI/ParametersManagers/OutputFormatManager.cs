using TMPro;
using UnityEngine;
using System;
using UnityEditor.Search;

public class OutputFormatManager : MonoBehaviour, IAPIParameterManager, IDefaultable
{
    [SerializeField] TMP_Dropdown _outputFormatDropdown;

    enum InputType
    {
        Json,
        Text
    }

    public bool TryGetURL(out string URL)
    {
        URL = "format=";

        InputType inputType = (InputType)_outputFormatDropdown.value;
        switch (inputType)
        {
            case InputType.Json:
                URL += "json";
                return true;
            case InputType.Text:
                URL += "text";
                return true;
        }

        return false;
    }

    public void ApplyDefault()
    {
        _outputFormatDropdown.value = 0;
        _outputFormatDropdown.RefreshShownValue();
    }
}
