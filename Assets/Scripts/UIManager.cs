using UnityEngine;
using TMPro;
using Unity.AppUI.UI;

public class UIManager : MonoBehaviour
{
    [Header("Simulation State UI")]
    [SerializeField] TextMeshProUGUI _simulationTimePassedText;
    [SerializeField] TextMeshProUGUI _dateTimeText;
    [Header("Simulation Settings UI")]
    [SerializeField] TextMeshProUGUI _timeScaleText;
    [SerializeField] TextMeshProUGUI _gravityScaleText;

    [Header("Speed")]
    [SerializeField] MovementController _movementController;
    [SerializeField] TextMeshProUGUI _speedText;

    // Update is called once per frame
    void Update()
    {
        if (_speedText != null && _movementController != null)
            _speedText.text = $"Speed: {_movementController.SpeedKmPerSec} km/s";

        if (_timeScaleText != null)
            _timeScaleText.text = $"Time Scale: {SimulationSettings.Instance.TimeScale:F1}x";

        if (_gravityScaleText != null)
            _gravityScaleText.text = $"Gravity Scale: {SimulationSettings.Instance.GravityScale:F1}x";

        var t = System.TimeSpan.FromSeconds(SimulationSettings.Instance.SimSeconds);
        if (_simulationTimePassedText != null)
            _simulationTimePassedText.text = $"Sim Time: {SimulationSettings.Instance.SimDays:F3} days  ({t:dd\\.hh\\:mm\\:ss})";

        if (_dateTimeText != null)
            _dateTimeText.text = $"Date Time: {SimulationSettings.Instance.GetCurrentDateTime()}";

    }
}
