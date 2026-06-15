using UnityEngine;
using TMPro;

public class LiveSimulationInformationPanelManager : MonoBehaviour
{
    public static LiveSimulationInformationPanelManager Instance { get; private set; }

    [SerializeField] GameObject _panelContainer;
    [SerializeField] GameObject _temperatureContainer;

    [SerializeField] TextMeshProUGUI _headerText;

    [SerializeField] TextMeshProUGUI _nameText;
    [SerializeField] TextMeshProUGUI _typeText;
    [SerializeField] TextMeshProUGUI _massText;
    [SerializeField] TextMeshProUGUI _diameterText;
    [SerializeField] TextMeshProUGUI _temperatureText;

    [SerializeField] TextMeshProUGUI _positionXText;
    [SerializeField] TextMeshProUGUI _positionYText;
    [SerializeField] TextMeshProUGUI _positionZText;

    [SerializeField] TextMeshProUGUI _velocityXText;
    [SerializeField] TextMeshProUGUI _velocityYText;
    [SerializeField] TextMeshProUGUI _velocityZText;

    AstronomicalObject _astronomicalObject;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Update()
    {
        if (_astronomicalObject == null) return;
        if (NBodyManager.Instance == null || NBodyManager.Instance.IsPaused) return;

        if (_astronomicalObject != null)
        {
            _positionXText.text = $"{_astronomicalObject.Position.x}";
            _positionYText.text = $"{_astronomicalObject.Position.y}";
            _positionZText.text = $"{_astronomicalObject.Position.z}";

            _velocityXText.text = $"{_astronomicalObject.Velocity.x}";
            _velocityYText.text = $"{_astronomicalObject.Velocity.y}";
            _velocityZText.text = $"{_astronomicalObject.Velocity.z}";
        }
    }

    public void Initialize(AstronomicalObject astronomicalObject)
    {
        _panelContainer.SetActive(true);

        _astronomicalObject = astronomicalObject;

        _headerText.text = $"'{astronomicalObject.Data.Body.Name}' Information";

        _nameText.text = $"{astronomicalObject.Data.Body.Name}";
        _typeText.text = $"{astronomicalObject.Data.Body.Type}";
        _massText.text = $"{astronomicalObject.Data.Body.Mass:E2} KG";
        _diameterText.text = $"{(astronomicalObject.Data.Body.Diameter / 1000):E2} KM";

        if (astronomicalObject.Data.Body.Type != BodyType.Star) _temperatureContainer.SetActive(false);
        else
        {
            _temperatureContainer.SetActive(true);
            _temperatureText.text = $"{astronomicalObject.Data.Body.Temperature} K";
        }

        _positionXText.text = $"{_astronomicalObject.Position.x}";
        _positionYText.text = $"{_astronomicalObject.Position.y}";
        _positionZText.text = $"{_astronomicalObject.Position.z}";
        _velocityXText.text = $"{_astronomicalObject.Velocity.x}";
        _velocityYText.text = $"{_astronomicalObject.Velocity.y}";
        _velocityZText.text = $"{_astronomicalObject.Velocity.z}";
    }

    public void OnClickCloseButton()
    {
        _astronomicalObject = null;
        _panelContainer.SetActive(false);
    }
}
