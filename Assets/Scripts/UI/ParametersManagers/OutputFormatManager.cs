using TMPro;
using UnityEngine;

public class OutputFormatManager : MonoBehaviour, IAPIParameterManager, IDefaultable
{
    [SerializeField] TMP_Dropdown _outputFormatDropdown;

    public bool TryGetURL(out string URL)
    {
        URL = "format=";

        switch (_outputFormatDropdown.value)
        {
            // json
            case 0:
                URL += "json";
                return true;
            // text
            case 1:
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
