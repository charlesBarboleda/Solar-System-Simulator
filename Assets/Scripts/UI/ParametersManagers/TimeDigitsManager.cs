using TMPro;
using Unity.InferenceEngine;
using UnityEngine;

public class TimeDigitsManager : MonoBehaviour, IAPIParameterManager, IDefaultable
{
    [SerializeField] TMP_Dropdown _timeDigitsDropdown;

    public bool TryGetURL(out string URL)
    {
        TimeDigitsDropdown selection = (TimeDigitsDropdown)_timeDigitsDropdown.value;
        string value = selection.ToString().ToUpperInvariant();
        URL = $"TIME_DIGITS={HorizonsAPIParameters.EncodeQuoted(value)}";
        return true;
    }

    public void ApplyDefault()
    {
        _timeDigitsDropdown.value = 0;
        _timeDigitsDropdown.RefreshShownValue();
    }

    enum TimeDigitsDropdown
    {
        Minutes,
        Seconds,
        FracSec
    }

}