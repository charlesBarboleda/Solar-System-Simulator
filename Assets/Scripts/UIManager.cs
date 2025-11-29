using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("Simulation Settings UI")]
    [SerializeField] private TextMeshProUGUI _timeScaleText;
    [SerializeField] private TextMeshProUGUI _gravityScaleText;

    // Update is called once per frame
    void Update()
    {

        if (_timeScaleText != null)
            _timeScaleText.text = $"Time Scale: {SimulationSettings.Instance.TimeScale:F1}x";

        if (_gravityScaleText != null)
            _gravityScaleText.text = $"Gravity Scale: {SimulationSettings.Instance.GravityScale:F1}x";

    }
}
