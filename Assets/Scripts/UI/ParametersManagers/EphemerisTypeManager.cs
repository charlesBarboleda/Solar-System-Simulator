using System;
using TMPro;
using UnityEngine;

public class EphemerisTypeManager : MonoBehaviour, IAPIParameterManager, IDefaultable
{
    [SerializeField] TMP_Dropdown _ephemTypeDropdown;

    enum InputType
    {
        Vectors,
        Observer,
        Elements,
        SPK,
        Approach
    }

    public bool TryGetURL(out string URL)
    {
        URL = "EPHEM_TYPE=";
        string value;
        InputType inputType = (InputType)_ephemTypeDropdown.value;

        switch (inputType)
        {
            case InputType.Vectors:
                value = Uri.EscapeDataString("'VECTORS'");
                URL += value;
                return true;
            case InputType.Observer:
                value = Uri.EscapeDataString("'OBSERVER'");
                URL += value;
                return true;
            case InputType.Elements:
                value = Uri.EscapeDataString("'ELEMENTS'");
                URL += value;
                return true;
            case InputType.SPK:
                value = Uri.EscapeDataString("'SPK'");
                URL += value;
                return true;
            case InputType.Approach:
                value = Uri.EscapeDataString("'APPROACH'");
                URL += value;
                return true;
        }

        return false;
    }

    public void ApplyDefault()
    {
        _ephemTypeDropdown.value = 0;
        _ephemTypeDropdown.RefreshShownValue();
    }
}
