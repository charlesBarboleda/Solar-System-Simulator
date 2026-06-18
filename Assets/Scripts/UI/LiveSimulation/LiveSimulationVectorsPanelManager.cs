using TMPro;
using Unity.Mathematics;
using UnityEngine;

public class LiveSimulationVectorsPanelManager : MonoBehaviour
{
    public static LiveSimulationVectorsPanelManager Instance { get; private set; }

    [SerializeField] GameObject _vectorsPanelContainer;

    [SerializeField] TextMeshProUGUI _headerText;

    [SerializeField] TMP_InputField _posXInput;
    [SerializeField] TMP_InputField _posYInput;
    [SerializeField] TMP_InputField _posZInput;

    [SerializeField] TMP_InputField _velXInput;
    [SerializeField] TMP_InputField _velYInput;
    [SerializeField] TMP_InputField _velZInput;


    AstronomicalObject _astronomicalObject;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        _astronomicalObject = null;
    }

    public void OpenPanel(AstronomicalObject astroObject)
    {
        _astronomicalObject = astroObject;

        Initialize();

        _vectorsPanelContainer.SetActive(true);
    }

    public void OnCloseButtonClick()
    {
        _vectorsPanelContainer.SetActive(false);
    }

    public void OnApplyButtonClick()
    {
        UIMessage.Instance.NewUIConfirmation(
            $"Are you sure you want to apply these vectors?",
            onYes: () =>
            {
                ApplyVectors();
            },
            onNo: () => { }
        );
    }

    void ApplyVectors()
    {
        if (_astronomicalObject == null)
        {
            Debug.Log($"[LiveSimulationVectorsPanelManager] ApplyVectors(): Astronomical Object reference is null");
            return;
        }

        double3 newPosition = new();
        double3 newVelocity = new();

        bool noPositionChanges = true;
        bool noVelocityChanges = true;

        // Position
        if (string.IsNullOrWhiteSpace(_posXInput.text))
            newPosition.x = _astronomicalObject.Position.x;
        else
        {
            double.TryParse(_posXInput.text, out newPosition.x);
            noPositionChanges = false;
        }

        if (string.IsNullOrWhiteSpace(_posYInput.text))
            newPosition.y = _astronomicalObject.Position.y;
        else
        {
            double.TryParse(_posYInput.text, out newPosition.y);
            noPositionChanges = false;
        }

        if (string.IsNullOrWhiteSpace(_posZInput.text))
            newPosition.z = _astronomicalObject.Position.z;
        else
        {
            double.TryParse(_posZInput.text, out newPosition.z);
            noPositionChanges = false;
        }


        // Velocity
        if (string.IsNullOrWhiteSpace(_velXInput.text))
            newVelocity.x = _astronomicalObject.Velocity.x;
        else
        {
            double.TryParse(_velXInput.text, out newPosition.x);
            noVelocityChanges = false;
        }

        if (string.IsNullOrWhiteSpace(_velYInput.text))
            newVelocity.y = _astronomicalObject.Velocity.y;
        else
        {
            double.TryParse(_velYInput.text, out newPosition.y);
            noVelocityChanges = false;
        }

        if (string.IsNullOrWhiteSpace(_velZInput.text))
            newVelocity.z = _astronomicalObject.Velocity.z;
        else
        {
            double.TryParse(_velZInput.text, out newPosition.z);
            noVelocityChanges = false;
        }

        if (!noPositionChanges)
        {
            if (_astronomicalObject.SetPosition(newPosition))
                UIMessage.Instance.NewFadingMessage(MessageType.Success, $"Applied new Position to {_astronomicalObject.Data.Body.Name}");
        }

        if (!noVelocityChanges)
        {
            UIMessage.Instance.NewFadingMessage(MessageType.Success, $"Applied new Velocity to {_astronomicalObject.Data.Body.Name}");
            _astronomicalObject.SetVelocity(newVelocity);
        }


    }

    void Initialize()
    {
        if (_astronomicalObject == null)
        {
            Debug.Log($"[LiveSimulationVectorsPanelManager] Initialize(): Astronomical Object reference is null");
            return;
        }

        SetHeaderText();
    }

    void SetHeaderText()
    {
        string truncatedName = TruncateWord(_astronomicalObject.Data.Body.Name, 8);

        _headerText.SetText($"'{truncatedName}' Vectors");
    }

    string TruncateWord(string word, int maxLength)
    {
        if (word.Length <= maxLength)
            return word;

        return word[..maxLength] + "...";
    }



}
