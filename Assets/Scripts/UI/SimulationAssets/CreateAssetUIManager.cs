using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NaughtyAttributes;

public class CreateAssetUIManager : MonoBehaviour
{
    [SerializeField] GameObject _createAssetPanel;
    [SerializeField] GameObject _nameContainer;
    [SerializeField] GameObject _diameterContainer;
    [SerializeField] GameObject _massContainer;
    [SerializeField] GameObject _bodyTypeContainer;
    [SerializeField] GameObject _materialContainer;
    [SerializeField] GameObject _temperatureContainer;
    [SerializeField] GameObject _hasRingToggleContainer;
    [SerializeField] GameObject _ringInnerGapContainer;
    [SerializeField] GameObject _ringWidthContainer;

    [SerializeField] TMP_InputField _nameInputField;
    [SerializeField] TMP_InputField _diameterInputField;
    [SerializeField] TMP_InputField _massInputField;
    [SerializeField] TMP_Dropdown _bodyTypeDropdown;
    [SerializeField] TMP_Dropdown _materialDropdown;
    [SerializeField] TMP_InputField _temperatureInputField;
    [SerializeField] Toggle _hasRingToggle;
    [SerializeField] TMP_InputField _ringInnerGapInputField;
    [SerializeField] TMP_InputField _ringWidthInputField;

    // Rotation Mode Button Text
    [SerializeField] TextMeshProUGUI _rotationModeButtonText;

    // Add rotation advanced
    [SerializeField] GameObject _rotRateInputContainer;
    [SerializeField] TMP_InputField _rotRateInput;

    [SerializeField] GameObject _meanSiderealDayInputContainer;
    [SerializeField] TMP_InputField _meanSiderealDayInput;

    [SerializeField] GameObject _axialTiltInputContainer;
    [SerializeField] TMP_InputField _axialTiltInput;

    [SerializeField] GameObject _axisAzimuthInputContainer;
    [SerializeField] TMP_InputField _axisAzimuthInput;

    [SerializeField] GameObject _initialSpinInputContainer;
    [SerializeField] TMP_InputField _initialSpinInput;

    [SerializeField] GameObject _primeMeridianOffsetInputContainer;
    [SerializeField] TMP_InputField _primeMeridianOffsetInput;

    [SerializeField] GameObject _retrogradeToggleContainer;
    [SerializeField] Toggle _retrogradeToggle;

    // Add rotation basic
    [SerializeField] GameObject _rotPeriodInputContainer;
    [SerializeField] TMP_InputField _rotPeriodInput;

    bool _isBasicRotation = true;

    void Awake()
    {
        _isBasicRotation = true;
    }

    Data CreateData()
    {
        BodyData bodyData = CreateBodyData();
        VisualData visualData = CreateVisualData();
        RotationData rotationData = CreateRotationData();

        bool isRingPlanet = bodyData.Type == BodyType.Planet && _hasRingToggleContainer.activeInHierarchy && _hasRingToggle.isOn;
        DisplayData displayData = CreateDisplayData(bodyData, visualData, isRingPlanet);

        if (_hasRingToggleContainer.activeInHierarchy && _hasRingToggle.isOn)
        {
            if (ValidateRingProperties())
            {
                RingData ringData = CreateRingData();

                Data dataWithRing = new()
                {
                    Body = bodyData,
                    Visual = visualData,
                    Display = displayData,
                    Ring = ringData
                };

                return dataWithRing;
            }
        }

        Data data = new()
        {
            Body = bodyData,
            Rotation = rotationData,
            Visual = visualData,
            Display = displayData
        };

        return data;
    }

    RingData CreateRingData()
    {
        RingData ringData = new()
        {
            IsRingPlanet = _hasRingToggle.isOn,
            InnerGapKM = double.TryParse(_ringInnerGapInputField.text, out double innerGap) ? innerGap : 0,
            RingWidthKM = double.TryParse(_ringWidthInputField.text, out double ringWidth) ? ringWidth : 0
        };

        return ringData;
    }

    DisplayData CreateDisplayData(BodyData bodyData, VisualData visualData, bool isRingPlanet = false)
    {
        GameObject go;

        if (isRingPlanet) go = AstronomicalObjectFactory.Instance.CreateEmptyAstroObject(BodyType.Planet, isRingPlanet: true);
        else go = AstronomicalObjectFactory.Instance.CreateEmptyAstroObject(bodyData.Type);

        switch (bodyData.Type)
        {
            case BodyType.Planet:
                go.TryGetComponent(out MeshRenderer renderer);

                if (renderer != null && AstronomicalObjectFactory.Instance.TryGetMaterial(visualData.MaterialName, out Material material))
                {
                    renderer.material = material;
                }
                break;

            case BodyType.Star:
                if (go.TryGetComponent(out SunRenderingManager sunManager))
                    sunManager.InitializeForDisplay(bodyData);
                break;
        }

        RuntimePreviewGenerator.MarkTextureNonReadable = false;

        bool isStarBody = bodyData.Type == BodyType.Star;

        Texture2D previewImage = RuntimePreviewGenerator.GenerateModelPreview(
            go.transform,
            width: 512,
            height: 512,
            shouldCloneModel: false,
            shouldIgnoreParticleSystems: false,
            isStarBody: isStarBody);

        RuntimePreviewGenerator.MarkTextureNonReadable = true;

        Destroy(go);

        return new DisplayData { DisplayImage = previewImage };
    }
    BodyData CreateBodyData()
    {
        BodyData bodyData = new()
        {
            Name = _nameInputField.text,
            Diameter = double.TryParse(_diameterInputField.text, out double diameter) ? diameter : 0,
            Mass = double.TryParse(_massInputField.text, out double mass) ? mass : 0,
            Type = (BodyType)_bodyTypeDropdown.value,
            Temperature = double.TryParse(_temperatureInputField.text, out double temperature) ? temperature : 0,
        };

        return bodyData;
    }

    VisualData CreateVisualData()
    {
        VisualData visualData = new()
        {
            MaterialName = _materialDropdown.options[_materialDropdown.value].text
        };

        return visualData;
    }

    RotationData CreateRotationData(bool isBasic = false)
    {
        RotationData rotationData = new();

        if (!isBasic)
        {
            if (!string.IsNullOrEmpty(_rotRateInput.text))
            {
                if (!double.TryParse(_rotRateInput.text, out double rotRate))
                {
                    UIMessage.Instance.NewFadingMessage(MessageType.Error, $"Rotation Rate input '{_rotRateInput.text}' is invalid!");
                }
                else rotationData.RotationRate = rotRate;
            }

            if (!string.IsNullOrEmpty(_meanSiderealDayInput.text))
            {
                if (!double.TryParse(_meanSiderealDayInput.text, out double meanSiderealDay))
                {
                    UIMessage.Instance.NewFadingMessage(MessageType.Error, $"Mean Sidereal Day input '{_meanSiderealDayInput.text}' is invalid!");
                }
                else rotationData.MeanSiderealDay = meanSiderealDay;
            }

            if (!string.IsNullOrEmpty(_axialTiltInput.text))
            {
                if (!double.TryParse(_axialTiltInput.text, out double axialTilt))
                {
                    UIMessage.Instance.NewFadingMessage(MessageType.Error, $"Axial Tilt input '{_axialTiltInput.text}' is invalid!");
                }
                else rotationData.AxialTiltDeg = axialTilt;
            }

            if (!string.IsNullOrEmpty(_axisAzimuthInput.text))
            {
                if (!double.TryParse(_axisAzimuthInput.text, out double axisAzimuth))
                {
                    UIMessage.Instance.NewFadingMessage(MessageType.Error, $"Axis Azimuth input '{_axisAzimuthInput.text}' is invalid!");
                }
                else rotationData.AxisAzimuthDeg = axisAzimuth;
            }

            if (!string.IsNullOrEmpty(_initialSpinInput.text))
            {
                if (!double.TryParse(_initialSpinInput.text, out double initialSpin))
                {
                    UIMessage.Instance.NewFadingMessage(MessageType.Error, $"Initial Spin input '{_initialSpinInput.text}' is invalid!");
                }
                else rotationData.InitialSpinDeg = initialSpin;
            }

            if (!string.IsNullOrEmpty(_primeMeridianOffsetInput.text))
            {
                if (!double.TryParse(_primeMeridianOffsetInput.text, out double primeMeridianOffset))
                {
                    UIMessage.Instance.NewFadingMessage(MessageType.Error, $"Prime Meridian Offset input '{_primeMeridianOffsetInput.text}' is invalid!");
                }
                else rotationData.ModelPrimeMeridianOffset = primeMeridianOffset;
            }

            rotationData.Retrograde = _retrogradeToggle.isOn;

            rotationData.IsBasicRotation = false;

            return rotationData;
        }
        else
        {
            if (!string.IsNullOrEmpty(_rotPeriodInput.text))
            {
                if (!double.TryParse(_rotPeriodInput.text, out double rotationPeriod))
                {
                    UIMessage.Instance.NewFadingMessage(MessageType.Error, $"Rotation Period input '{_rotPeriodInput.text}' is invalid!");
                }
                else rotationData.RotationPeriod = rotationPeriod * 24f;
            }

            if (!string.IsNullOrEmpty(_axialTiltInput.text))
            {
                if (!double.TryParse(_axialTiltInput.text, out double axialTilt))
                {
                    UIMessage.Instance.NewFadingMessage(MessageType.Error, $"Axial Tilt input '{_axialTiltInput.text}' is invalid!");
                }
                else rotationData.AxialTiltDeg = axialTilt;
            }

            rotationData.Retrograde = _retrogradeToggle.isOn;

            rotationData.IsBasicRotation = true;

            return rotationData;
        }
    }

    public void OnRotationModeButtonClick()
    {
        _isBasicRotation = !_isBasicRotation;

        if (_isBasicRotation)
        {
            _rotationModeButtonText.SetText("Basic");

            _rotPeriodInputContainer.SetActive(true);
            _axialTiltInputContainer.SetActive(true);
            _retrogradeToggleContainer.SetActive(true);

            _rotRateInputContainer.SetActive(false);
            _meanSiderealDayInputContainer.SetActive(false);
            _axisAzimuthInputContainer.SetActive(false);
            _initialSpinInputContainer.SetActive(false);
            _primeMeridianOffsetInputContainer.SetActive(false);
        }
        else
        {
            _rotationModeButtonText.SetText("Advanced");

            _rotRateInputContainer.SetActive(true);
            _meanSiderealDayInputContainer.SetActive(true);
            _axisAzimuthInputContainer.SetActive(true);
            _initialSpinInputContainer.SetActive(true);
            _primeMeridianOffsetInputContainer.SetActive(true);
            _retrogradeToggleContainer.SetActive(true);
            _axialTiltInputContainer.SetActive(true);

            _rotPeriodInputContainer.SetActive(false);
        }

    }


    public void OnToggleHasRing(bool hasRing)
    {
        _ringInnerGapContainer.SetActive(hasRing);
        _ringWidthContainer.SetActive(hasRing);
    }

    public void OnBodyTypeDropdownValueChanged(int index)
    {
        _nameContainer.SetActive(true);
        _diameterContainer.SetActive(true);
        _massContainer.SetActive(true);

        BodyType bodyType = (BodyType)index;

        switch (bodyType)
        {
            case BodyType.Planet:
                _materialContainer.SetActive(true);
                _temperatureContainer.SetActive(false);
                _hasRingToggleContainer.SetActive(true);
                break;
            case BodyType.Star:
                _temperatureContainer.SetActive(true);
                _materialContainer.SetActive(false);
                _hasRingToggleContainer.SetActive(false);
                _ringInnerGapContainer.SetActive(false);
                _ringWidthContainer.SetActive(false);
                break;
            case BodyType.Asteroid:
            case BodyType.Moon:
            case BodyType.Satellite:
                _materialContainer.SetActive(false);
                _temperatureContainer.SetActive(false);
                _hasRingToggleContainer.SetActive(false);
                _ringInnerGapContainer.SetActive(false);
                _ringWidthContainer.SetActive(false);
                break;
        }
    }


    public void OnCreateAssetClick()
    {
        _createAssetPanel.SetActive(true);
    }

    public void OnCreateAssetCloseButtonClick()
    {
        _createAssetPanel.SetActive(false);
    }

    public void OnCreateClick()
    {
        Data data = CreateData();

        if (data.Body.Type == BodyType.Star && !ValidateTemperature()) return;

        AstronomicalObjectFactory.Instance.CreateAstronomicalObject(data, AddToRuntime: false, AddToAssetDatabase: true);

        if (!SimulationAssetDatabaseManager.Instance.TryGetBodyByName(data.Body.Name, out _))
        {
            UIMessage.Instance.NewUIMessage(MessageType.Error, "Failed to create asset. Check input values and try again.", "Task Failed");
            return;
        }

        UIMessage.Instance.NewUIMessage(MessageType.Success, $"Successfully created asset: {data.Body.Name}", "Task Complete");

        ResetForm();
        _createAssetPanel.SetActive(false);
    }



    void ResetForm()
    {
        _nameInputField.text = string.Empty;
        _diameterInputField.text = string.Empty;
        _massInputField.text = string.Empty;
        _temperatureInputField.text = string.Empty;

        _bodyTypeDropdown.value = 0;
        _materialDropdown.value = 0;

        OnBodyTypeDropdownValueChanged(0);
    }

    bool ValidateRingProperties()
    {
        bool isValid = true;

        if (!double.TryParse(_ringInnerGapInputField.text, out double innerGap) || innerGap < 0)
        {
            UIMessage.Instance.NewUIMessage(MessageType.Error, "Invalid ring inner gap input. Please enter a valid non-negative number.", "Input Error");
            isValid = false;
        }

        if (!double.TryParse(_ringWidthInputField.text, out double ringWidth) || ringWidth <= 0)
        {
            UIMessage.Instance.NewUIMessage(MessageType.Error, "Invalid ring width input. Please enter a valid positive number.", "Input Error");
            isValid = false;
        }

        return isValid;
    }

    bool ValidateTemperature()
    {
        if (_temperatureInputField.text == string.Empty) return false;

        if (!double.TryParse(_temperatureInputField.text, out double temperature) || temperature < 2500)
        {
            UIMessage.Instance.NewUIMessage(MessageType.Error, "Invalid temperature input. Please enter a valid temperature greater than 2500.", "Input Error");
            return false;
        }

        return true;
    }
}
