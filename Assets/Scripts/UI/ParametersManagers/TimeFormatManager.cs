using TMPro;
using UnityEngine;
using NaughtyAttributes;
public class TimeFormatManager : MonoBehaviour, IDefaultable
{
    [SerializeField] TMP_Dropdown _timeFormatDropdown;

    // Range DateTime Parameter containers
    [SerializeReference] GameObject _startTimeContainer;
    [SerializeReference] GameObject _stopTimeContainer;
    [SerializeReference] GameObject _stepSizeContainer;

    // Specific DateTime Parameter containers
    [SerializeReference] GameObject _tListContainer;

    [SerializeField] GameObject _parameterContainer;

    public void ApplyDefault()
    {
        _timeFormatDropdown.value = 0;
        _timeFormatDropdown.RefreshShownValue();
    }

    public void ChangeParameters()
    {
        switch (GetTimeFormat())
        {
            case TimeFormat.Range:
                _startTimeContainer.SetActive(true);
                _stopTimeContainer.SetActive(true);
                _stepSizeContainer.SetActive(true);

                _tListContainer.SetActive(false);
                return;
            case TimeFormat.Specific:
                _tListContainer.SetActive(true);

                _startTimeContainer.SetActive(false);
                _stopTimeContainer.SetActive(false);
                _stepSizeContainer.SetActive(false);
                return;
        }
    }

    public void OnDropdownChange(int idx)
    {
        ChangeParameters();
    }

    public TimeFormat GetTimeFormat() => (TimeFormat)_timeFormatDropdown.value;

    public GameObject GetParameterContainer() => _parameterContainer;

}

public enum TimeFormat
{
    Range,
    Specific
}
