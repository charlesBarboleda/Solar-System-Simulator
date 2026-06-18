using UnityEngine;
using TMPro;
using System;

public class UIManager : MonoBehaviour
{
    [Header("Simulation State UI")]
    [SerializeField] TextMeshProUGUI _simulationTimePassedText;
    [SerializeField] TextMeshProUGUI _dateTimeText;

    [Header("Speed")]
    [SerializeField] MovementController _movementController;
    [SerializeField] TextMeshProUGUI _speedText;
    void Update()
    {
        if (_speedText != null && _movementController != null)
            _speedText.SetText($"Speed: {_movementController.SpeedKmPerSec} km/s");

        var t = TimeSpan.FromSeconds(SimulationSettings.Instance.SimSeconds);
        if (_simulationTimePassedText != null)
            _simulationTimePassedText.SetText($"Sim Time Elapsed: {SimulationSettings.Instance.SimDays:F3} days  ({t:dd\\.hh\\:mm\\:ss})");

        if (_dateTimeText != null)
            _dateTimeText.SetText($"Date Time: {SimulationSettings.Instance.GetCurrentDateTime()}");

    }
}
