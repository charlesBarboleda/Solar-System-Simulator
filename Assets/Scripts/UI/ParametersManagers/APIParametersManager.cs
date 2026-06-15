using UnityEngine;

public class APIParametersManager : MonoBehaviour
{
    public const string HORIZONS_BASE_URL = "https://ssd.jpl.nasa.gov/api/horizons.api?";

    [Header("Parameter Managers references")]
    [SerializeField] OutputFormatManager _outputFormatManager;
    [SerializeField] CenterBodyManager _centerBodyManager;
    [SerializeField] MainBodyNAIFManager _mainBodyManager;
    [SerializeField] StartTimeManager _startTimeManager;
    [SerializeField] StopTimeManager _stopTimeManager;
    [SerializeField] TListManager _tListManager;

    void Awake()
    {
        if (_tListManager == null)
        {
            Debug.LogError("No 'TListManager' assigned in inspector");
            enabled = false;
            return;
        }

        if (_outputFormatManager == null)
        {
            Debug.LogError("No 'OutputFormatManager' assigned in inspector");
            enabled = false;
            return;
        }
    }

    public string URLBuilder()
    {
        string URL = string.Empty;



        return URL;
    }


}
