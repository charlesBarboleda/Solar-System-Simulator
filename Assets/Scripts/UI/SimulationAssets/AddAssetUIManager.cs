using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Mathematics;
using System.Collections.Generic;

public class AddAssetUIManager : MonoBehaviour
{
    public static AddAssetUIManager Instance { get; private set; }

    // Containers
    [SerializeField] GameObject _addAssetContainer;

    // Header
    [SerializeField] TextMeshProUGUI _headerText;

    // Position inputs
    [SerializeField] TMP_Dropdown _relativeToDropdown;
    [SerializeField] TMP_InputField _posXInput;
    [SerializeField] TMP_InputField _posYInput;
    [SerializeField] TMP_InputField _posZInput;

    // Velocity inputs
    [SerializeField] TMP_InputField _velXInput;
    [SerializeField] TMP_InputField _velYInput;
    [SerializeField] TMP_InputField _velZInput;

    Data _data;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        PopulateDropdown();
    }

    public void OnAddClick()
    {
        _data.Position = CreatePositionData();
        _data.Velocity = CreateVelocityData();

        AstronomicalObjectFactory.Instance.CreateAstronomicalObject(_data, AddToRuntime: true, AddToAssetDatabase: false);

        _addAssetContainer.SetActive(false);
    }


    public void CloseContainer()
    {
        _data = default;
        _addAssetContainer.SetActive(false);
    }

    public void OpenContainer(Data data)
    {
        _data = data;
        SetHeaderText();
        _addAssetContainer.SetActive(true);
    }

    void SetHeaderText()
    {
        string objectName = _data.Body.Name;
        string truncatedName = TruncateWord(objectName, 8);

        _headerText.SetText($"Add '{truncatedName}' to Simulation");
    }

    string TruncateWord(string word, int maxLength)
    {
        if (word.Length <= maxLength)
            return word;

        return word[..maxLength] + "...";
    }

    PositionData CreatePositionData()
    {
        PositionData positionData = new();

        // Handle reference object
        SimulationObject relativeObject = null;

        string relativeTo = _relativeToDropdown.options[_relativeToDropdown.value].text;

        if (relativeTo == "Player")
        {
            relativeObject = MovementController.Instance;
        }
        else
        {
            if (!NBodyManager.Instance.TryGetAstroObjectByName(relativeTo, out AstronomicalObject astroObject))
            {
                UIMessage.Instance.NewFadingMessage(MessageType.Error, $"Could not find reference object '{relativeTo}'!");

                return positionData;
            }

            relativeObject = astroObject;
        }

        if (relativeObject == null)
        {
            UIMessage.Instance.NewFadingMessage(MessageType.Error, $"Relative object '{relativeTo}' is null!");

            return positionData;
        }

        double xAU = 0;
        double yAU = 0;
        double zAU = 0;

        if (!string.IsNullOrWhiteSpace(_posXInput.text))
        {
            if (!double.TryParse(_posXInput.text, out xAU))
            {
                UIMessage.Instance.NewFadingMessage(MessageType.Error, $"Position X input '{_posXInput.text}' is invalid!");
            }
        }

        if (!string.IsNullOrWhiteSpace(_posYInput.text))
        {
            if (!double.TryParse(_posYInput.text, out yAU))
            {
                UIMessage.Instance.NewFadingMessage(MessageType.Error, $"Position Y input '{_posYInput.text}' is invalid!");
            }
        }

        if (!string.IsNullOrWhiteSpace(_posZInput.text))
        {
            if (!double.TryParse(_posZInput.text, out zAU))
            {
                UIMessage.Instance.NewFadingMessage(MessageType.Error, $"Position Z input '{_posZInput.text}' is invalid!");
            }
        }

        // Convert AU offset -> Unity simulation units
        double3 relativeOffsetUnity = new(PhysicsConstants.ToUnityUnitsFromAU(xAU), PhysicsConstants.ToUnityUnitsFromAU(yAU), PhysicsConstants.ToUnityUnitsFromAU(zAU));

        // Calculate final global position
        double3 relativeObjectWorldPosition = relativeObject.GetGlobalPosition();
        double3 finalWorldPosition = relativeObjectWorldPosition + relativeOffsetUnity;

        positionData.StartPosition = finalWorldPosition;
        return positionData;
    }

    VelocityData CreateVelocityData()
    {
        VelocityData velocityData = new();

        double x = 0, y = 0, z = 0;

        if (!string.IsNullOrEmpty(_velXInput.text))
        {
            if (!double.TryParse(_velXInput.text, out x))
                UIMessage.Instance.NewFadingMessage(MessageType.Error, $"Velocity X input '{_velXInput.text}' is invalid!");
        }

        if (!string.IsNullOrEmpty(_velYInput.text))
        {
            if (!double.TryParse(_velYInput.text, out y))
                UIMessage.Instance.NewFadingMessage(MessageType.Error, $"Velocity Y input '{_velYInput.text}' is invalid!");
        }

        if (!string.IsNullOrEmpty(_velZInput.text))
        {
            if (!double.TryParse(_velZInput.text, out z))
                UIMessage.Instance.NewFadingMessage(MessageType.Error, $"Velocity Z input '{_velZInput.text}' is invalid!");
        }

        velocityData.StartVelocity = new double3(x, y, z);
        return velocityData;
    }

    public void PopulateDropdown()
    {
        _relativeToDropdown.ClearOptions();

        List<string> options = new()
        {
            "Player"
        };

        if (NBodyManager.Instance == null || NBodyManager.Instance.SystemBodies.Count == 0)
        {
            _relativeToDropdown.AddOptions(options);
            _relativeToDropdown.RefreshShownValue();
            return;
        }
        else
        {
            foreach (AstronomicalObject astroObject in NBodyManager.Instance.SystemBodies)
            {
                options.Add(astroObject.Data.Body.Name);
            }

            _relativeToDropdown.AddOptions(options);
            _relativeToDropdown.RefreshShownValue();
            return;
        }
    }
}
