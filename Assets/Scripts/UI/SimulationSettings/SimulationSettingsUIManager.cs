using TMPro;
using UnityEngine;

public class SimulationSettingsUIManager : MonoBehaviour
{
    public static SimulationSettingsUIManager Instance { get; private set; }

    [Header("Object Containers")]
    [SerializeField] GameObject _simulationSettingsPanel;

    [Header("Simulation Settings UI Input Fields")]
    [SerializeField] TMP_InputField _timeScaleInput;
    [SerializeField] TMP_InputField _gravityScaleInput;
    [SerializeField] TMP_InputField _fixedStepSimDaysInput;
    [SerializeField] TMP_InputField _maxSubstepsInput;
    [SerializeField] TMP_InputField _maxBacklogSimDaysInput;

    [Header("Simulation Settings UI Start DateTime Input Fields")]
    [SerializeField] TMP_InputField _startYearInput;
    [SerializeField] TMP_InputField _startMonthInput;
    [SerializeField] TMP_InputField _startDayInput;
    [SerializeField] TMP_InputField _startHourInput;
    [SerializeField] TMP_InputField _startMinuteInput;
    [SerializeField] TMP_InputField _startSecondInput;
    [SerializeField] TMP_InputField _startMillisecondInput;

    [Header("Placeholder Texts")]
    [SerializeField] TextMeshProUGUI _timeScalePlaceholder;
    [SerializeField] TextMeshProUGUI _gravityScalePlaceholder;
    [SerializeField] TextMeshProUGUI _fixedStepSimDaysPlaceholder;
    [SerializeField] TextMeshProUGUI _maxSubstepsPlaceholder;
    [SerializeField] TextMeshProUGUI _maxBacklogSimDaysPlaceholder;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void OnPresetDropdownChange(int idx)
    {
        SimulationSteppingPresets preset = (SimulationSteppingPresets)idx;

        SimulationSettings.Instance.HandleSimulationSteppingPreset(preset, out double fixedStep, out int maxSubstep, out double maxBacklog);

        _fixedStepSimDaysInput.text = fixedStep.ToString();
        _maxSubstepsInput.text = maxSubstep.ToString();
        _maxBacklogSimDaysInput.text = maxBacklog.ToString();
    }

    public void OnPlayButtonClick()
    {
        SimulationSettings.Instance.PlaySimulation();
        UIMessage.Instance.NewFadingMessage(MessageType.Info, "Simulation Started!");
    }

    public void OnPauseButtonClick()
    {
        SimulationSettings.Instance.PauseSimulation();
        UIMessage.Instance.NewFadingMessage(MessageType.Info, "Simulation Paused!");
    }

    public void SetPlaceholders()
    {
        _timeScalePlaceholder.text = $"Current: <b><size=19>{SimulationSettings.Instance.TimeScale}</size></b>";
        _gravityScalePlaceholder.text = $"Current: <b><size=19>{SimulationSettings.Instance.GravityScale}</size></b>";
        _fixedStepSimDaysPlaceholder.text = $"Current: <b><size=19>{SimulationSettings.Instance.FixedStepSimDays:E2}</size></b>";
        _maxSubstepsPlaceholder.text = $"Current: <b><size=19>{SimulationSettings.Instance.MaxSubstepsPerFixedUpdate:E2}</size></b>";
        _maxBacklogSimDaysPlaceholder.text = $"Current: <b><size=19>{SimulationSettings.Instance.MaxBacklogSimDays:E2}</size></b>";
    }

    public void OnCloseButtonClick() => _simulationSettingsPanel.SetActive(false);

    public void OnApplyClick()
    {
        if (!string.IsNullOrWhiteSpace(_timeScaleInput.text))
        {
            if (double.TryParse(_timeScaleInput.text, out double timeScale))
            {
                SimulationSettings.Instance.SetTimeScale(timeScale);
            }
        }

        if (!string.IsNullOrWhiteSpace(_gravityScaleInput.text))
        {
            if (double.TryParse(_gravityScaleInput.text, out double gravityScale))
            {
                SimulationSettings.Instance.SetGravityScale(gravityScale);
            }
        }

        if (!string.IsNullOrWhiteSpace(_fixedStepSimDaysInput.text))
        {
            if (double.TryParse(_fixedStepSimDaysInput.text, out double fixedStepSimDays))
            {
                SimulationSettings.Instance.SetFixedStepSimDays(fixedStepSimDays);
            }
        }

        if (!string.IsNullOrWhiteSpace(_maxSubstepsInput.text))
        {
            if (int.TryParse(_maxSubstepsInput.text, out int maxSubsteps))
            {
                SimulationSettings.Instance.SetMaxSubstepsPerFixedUpdate(maxSubsteps);
            }
        }

        if (!string.IsNullOrWhiteSpace(_maxBacklogSimDaysInput.text))
        {
            if (double.TryParse(_maxBacklogSimDaysInput.text, out double maxBacklogSimDays))
            {
                SimulationSettings.Instance.SetMaxBacklogSimDays(maxBacklogSimDays);
            }
        }

        if (string.IsNullOrWhiteSpace(_startYearInput.text) ||
            string.IsNullOrWhiteSpace(_startMonthInput.text) ||
            string.IsNullOrWhiteSpace(_startDayInput.text) ||
            string.IsNullOrWhiteSpace(_startHourInput.text) ||
            string.IsNullOrWhiteSpace(_startMinuteInput.text) ||
            string.IsNullOrWhiteSpace(_startSecondInput.text) ||
            string.IsNullOrWhiteSpace(_startMillisecondInput.text))
        {
            return;
        }

        if (!int.TryParse(_startYearInput.text, out int year) ||
             !int.TryParse(_startMonthInput.text, out int month) ||
             !int.TryParse(_startDayInput.text, out int day) ||
             !int.TryParse(_startHourInput.text, out int hour) ||
             !int.TryParse(_startMinuteInput.text, out int minute) ||
             !int.TryParse(_startSecondInput.text, out int second) ||
             !int.TryParse(_startMillisecondInput.text, out int millisecond))
        {
            return;
        }

        SimulationSettings.Instance.SetStartDateTime(
            year,
            month,
            day,
            hour,
            minute,
            second,
            millisecond
        );
    }

}
