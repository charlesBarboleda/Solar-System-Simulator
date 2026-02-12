using TMPro;
using UnityEngine;

public class TimeTypeManager : MonoBehaviour, IAPIParameterManager, IDefaultable
{
    [SerializeField] GameObject _parameterContainer;
    [SerializeField] TMP_Dropdown _timeTypeDropdown;
    public GameObject GetParameterContainer() => _parameterContainer;
    public void ApplyDefault()
    {
        _timeTypeDropdown.value = 0;
        _timeTypeDropdown.RefreshShownValue();
    }


    public bool TryGetURL(out string URL)
    {
        URL = "TIME_TYPE=";
        string value;

        TimeTypeDropdown timeType = (TimeTypeDropdown)_timeTypeDropdown.value;
        switch (timeType)
        {
            case TimeTypeDropdown.Universal:
                value = "UT";
                value = HorizonsAPIParameters.EncodeQuoted(value);
                URL += value;
                return true;
            case TimeTypeDropdown.Terrestrial:
                value = "TT";
                value = HorizonsAPIParameters.EncodeQuoted(value);
                URL += value;
                return true;
            case TimeTypeDropdown.Barycentric:
                value = "TDB";
                value = HorizonsAPIParameters.EncodeQuoted(value);
                URL += value;
                return true;
        }

        return false;
    }


    enum TimeTypeDropdown
    {
        Universal,
        Terrestrial,
        Barycentric
    }
}
