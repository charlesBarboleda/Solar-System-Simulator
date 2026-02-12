using System;
using System.Globalization;
using TMPro;
using UnityEngine;

public class CenterBodyManager : MonoBehaviour, IAPIParameterManager, IInputValidation, IDefaultable
{
    [SerializeField] TMP_Dropdown _inputTypeDropdown;
    [SerializeField] GameObject _parameterContainer;
    public GameObject ParameterContainer => _parameterContainer;

    [Header("SiteName / IAU Code references")]
    [SerializeField] TextMeshProUGUI _inputLabel;
    [SerializeField] TMP_InputField _inputField;
    [SerializeField] GameObject _invalidInputField;
    [SerializeField] TextMeshProUGUI _inputPlaceholderText;

    [Header("Container object references")]
    [SerializeField] GameObject _inputContainer;
    [SerializeField] GameObject _coordinateTypeContainer;
    [SerializeField] GameObject _siteCoordinateInputOneContainer;
    [SerializeField] GameObject _siteCoordinateInputTwoContainer;
    [SerializeField] GameObject _siteCoordinateInputThreeContainer;

    [SerializeField] GameObject _nameIAUContainer;
    [SerializeField] GameObject _IAUCodesButton;

    [Header("Site Coordinate references")]
    [SerializeField] TMP_Dropdown _coordinateTypeDropdown;
    [SerializeField] TMP_InputField _bodyNAIFInput;
    [SerializeField] GameObject _bodyNAIFContainer;
    [SerializeField] GameObject _invalidInputNAIFInput;
    [SerializeField] TMP_InputField _siteCoordInputOne;
    [SerializeField] GameObject _invalidInputOne;
    [SerializeField] TextMeshProUGUI _inputOneLabel;
    [SerializeField] TextMeshProUGUI _inputOnePlaceholderText;
    [SerializeField] TMP_InputField _siteCoordInputTwo;
    [SerializeField] GameObject _invalidInputTwo;
    [SerializeField] TextMeshProUGUI _inputTwoLabel;
    [SerializeField] TextMeshProUGUI _inputTwoPlaceholderText;
    [SerializeField] TMP_InputField _siteCoordInputThree;
    [SerializeField] GameObject _invalidInputThree;
    [SerializeField] TextMeshProUGUI _inputThreeLabel;
    [SerializeField] TextMeshProUGUI _inputThreePlaceholderText;

    public void OnNAIFDatabaseClick() => NAIFDatabaseUIController.Instance.OpenPanel(sortOrder: 2);
    public GameObject GetParameterContainer() => _parameterContainer;

    const string IAU_CODES_URL = "https://www.minorplanetcenter.net/iau/lists/ObsCodesF.html";

    public void OnCoordinateTypeChange(int idx)
    {
        CoordinateType coordinateType = (CoordinateType)idx;
        switch (coordinateType)
        {
            case CoordinateType.Geodetic:
                SwitchInputContents(_inputOneLabel, _inputOnePlaceholderText, label: "Longitude :", placeholder: "e.g. '236.8793'");
                SwitchInputContents(_inputTwoLabel, _inputTwoPlaceholderText, label: "Latitude :", placeholder: "e.g. '49.2827'");
                SwitchInputContents(_inputThreeLabel, _inputThreePlaceholderText, label: "Altitude :", placeholder: "e.g. '0.070'");
                break;
            case CoordinateType.Cylindrical:
                SwitchInputContents(_inputOneLabel, _inputOnePlaceholderText, label: "Longitude :", placeholder: "e.g. '236.8793'");
                SwitchInputContents(_inputTwoLabel, _inputTwoPlaceholderText, label: "DXY :", placeholder: "e.g. '4185.000'");
                SwitchInputContents(_inputThreeLabel, _inputThreePlaceholderText, label: "DZ :", placeholder: "e.g. '4815.000'");
                break;
        }
    }

    enum CoordinateType
    {
        Geodetic,
        Cylindrical
    }

    public void OnOneInputStartEdit()
    {
        if (_invalidInputOne.activeInHierarchy) _invalidInputOne.SetActive(false);
        return;
    }
    public void OnTwoInputStartEdit()
    {
        if (_invalidInputTwo.activeInHierarchy) _invalidInputTwo.SetActive(false);
        return;
    }
    public void OnThreeInputStartEdit()
    {
        if (_invalidInputThree.activeInHierarchy) _invalidInputThree.SetActive(false);
        return;
    }
    public void OnNameIAUInputStartEdit()
    {
        if (_invalidInputField.activeInHierarchy) _invalidInputField.SetActive(false);
        return;
    }
    public void OnNAIFInputStartEdit()
    {
        if (_invalidInputNAIFInput.activeInHierarchy) _invalidInputNAIFInput.SetActive(false);
        return;
    }

    public void OnInputTypeChange(int idx)
    {
        InputType inputType = (InputType)idx;
        switch (inputType)
        {
            case InputType.Geocenter:
                SetExclusive(InputType.Geocenter);
                break;
            case InputType.SiteName:
                SetExclusive(InputType.SiteName);
                break;
            case InputType.IAU:
                SetExclusive(InputType.IAU);
                break;
            case InputType.SiteCoordinate:
                SetExclusive(InputType.SiteCoordinate);
                break;
            default:
                SetExclusive(InputType.Geocenter);
                break;
        }
    }

    enum InputType
    {
        Geocenter,
        SiteName,
        IAU,
        SiteCoordinate
    }

    void SetExclusive(InputType inputType)
    {
        switch (inputType)
        {
            case InputType.Geocenter:
                _coordinateTypeContainer.SetActive(false);
                _bodyNAIFContainer.SetActive(false);
                _siteCoordinateInputOneContainer.SetActive(false);
                _siteCoordinateInputTwoContainer.SetActive(false);
                _siteCoordinateInputThreeContainer.SetActive(false);
                _nameIAUContainer.SetActive(false);
                _IAUCodesButton.SetActive(false);
                _inputContainer.SetActive(false);
                break;
            case InputType.SiteName:
                _inputContainer.SetActive(true);
                _nameIAUContainer.SetActive(true);
                SwitchInputContents(_inputLabel, _inputPlaceholderText, label: "Site Name :", placeholder: "e.g. 'Mauna Kea'");
                _inputField.characterLimit = 0;
                _bodyNAIFContainer.SetActive(false);
                _siteCoordinateInputOneContainer.SetActive(false);
                _siteCoordinateInputTwoContainer.SetActive(false);
                _siteCoordinateInputThreeContainer.SetActive(false);
                _coordinateTypeContainer.SetActive(false);
                break;
            case InputType.IAU:
                _inputContainer.SetActive(true);
                _IAUCodesButton.SetActive(true);
                _nameIAUContainer.SetActive(true);
                _coordinateTypeContainer.SetActive(false);
                _bodyNAIFContainer.SetActive(false);
                _siteCoordinateInputOneContainer.SetActive(false);
                _siteCoordinateInputTwoContainer.SetActive(false);
                _siteCoordinateInputThreeContainer.SetActive(false);
                SwitchInputContents(_inputLabel, _inputPlaceholderText, label: "IAU Code :", placeholder: "e.g. 'W84'");
                _inputField.characterLimit = 3;
                break;
            case InputType.SiteCoordinate:
                _inputContainer.SetActive(true);
                _bodyNAIFContainer.SetActive(true);
                _siteCoordinateInputOneContainer.SetActive(true);
                _siteCoordinateInputTwoContainer.SetActive(true);
                _siteCoordinateInputThreeContainer.SetActive(true);
                _coordinateTypeContainer.SetActive(true);
                _IAUCodesButton.SetActive(false);
                _nameIAUContainer.SetActive(false);
                break;
        }
    }

    void SwitchInputContents(TextMeshProUGUI labelText, TextMeshProUGUI placeholderText, string label, string placeholder)
    {
        labelText.SetText(label);
        placeholderText.SetText(placeholder);
    }

    public bool IsValidInput()
    {
        _invalidInputField.SetActive(false);
        InputType inputType = (InputType)_inputTypeDropdown.value;
        switch (inputType)
        {
            case InputType.Geocenter:
                return true;
            case InputType.IAU:
                string IAUInput = _inputField.text.Trim();
                if (string.IsNullOrEmpty(IAUInput)) return false;
                return true;
            case InputType.SiteName:
                string siteNameInput = _inputField.text.Trim();
                if (string.IsNullOrEmpty(siteNameInput)) return false;
                return true;
            case InputType.SiteCoordinate:
                string naifIDInput = _bodyNAIFInput.text.Trim();
                if (string.IsNullOrEmpty(naifIDInput))
                {
                    UIMessage.Instance.NewFadingMessage(MessageType.Error, $"[Center Body] Invalid input; Input cannot be empty", 20f);
                    _invalidInputNAIFInput.SetActive(true);
                    return false;
                }
                string inputOne = _siteCoordInputOne.text.Trim();
                if (string.IsNullOrEmpty(inputOne))
                {
                    UIMessage.Instance.NewFadingMessage(MessageType.Error, $"[Center Body] Invalid input; Input cannot be empty", 20f);
                    _invalidInputOne.SetActive(true);
                    return false;
                }
                string inputTwo = _siteCoordInputTwo.text.Trim();
                if (string.IsNullOrEmpty(inputTwo))
                {
                    UIMessage.Instance.NewFadingMessage(MessageType.Error, $"[Center Body] Invalid input; Input cannot be empty", 20f);
                    _invalidInputTwo.SetActive(true);
                    return false;
                }
                string inputThree = _siteCoordInputThree.text.Trim();
                if (string.IsNullOrEmpty(inputThree))
                {
                    UIMessage.Instance.NewFadingMessage(MessageType.Error, $"[Center Body] Invalid input; Input cannot be empty", 20f);
                    _invalidInputThree.SetActive(true);
                    return false;
                }

                return true;
        }

        return false;

    }

    public bool TryGetURL(out string URL)
    {
        URL = string.Empty;
        InputType inputType = (InputType)_inputTypeDropdown.value;

        if (!IsValidInput())
        {
            if (inputType == InputType.SiteName || inputType == InputType.IAU)
            {
                UIMessage.Instance.NewFadingMessage(MessageType.Error, "[Center Body] Invalid input; Input cannot be empty", 20f);

                if (_invalidInputField != null) _invalidInputField.SetActive(true);
            }

            return false;
        }

        switch (inputType)
        {
            case InputType.Geocenter:
                {
                    URL = $"CENTER={HorizonsAPIParameters.EncodeQuoted("geo")}";
                    return true;
                }

            case InputType.SiteName:
                {
                    string siteName = _inputField.text.Trim();
                    URL = $"CENTER={HorizonsAPIParameters.EncodeQuoted(siteName)}";
                    return true;
                }

            case InputType.IAU:
                {
                    string code = _inputField.text.Trim().ToUpperInvariant();

                    if (code.Length != 3)
                    {
                        UIMessage.Instance.NewFadingMessage(MessageType.Error, "[Center Body] IAU/MPC site code must be exactly 3 characters (e.g. W84).", 20f);

                        if (_invalidInputField != null) _invalidInputField.SetActive(true);

                        return false;
                    }

                    URL = $"CENTER={HorizonsAPIParameters.EncodeQuoted(code)}";
                    return true;
                }

            case InputType.SiteCoordinate:
                {
                    string naifRaw = _bodyNAIFInput.text.Trim();
                    if (!int.TryParse(naifRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int NAIFID))
                    {
                        UIMessage.Instance.NewFadingMessage(MessageType.Error, "[Center Body] Invalid NAIF ID; must be a positive integer (e.g. 399).", 20f);

                        if (_invalidInputNAIFInput != null) _invalidInputNAIFInput.SetActive(true);

                        return false;
                    }

                    CoordinateType coordType = (CoordinateType)_coordinateTypeDropdown.value;
                    string coordTypeToken = (coordType == CoordinateType.Geodetic) ? "GEODETIC" : "CYLINDRICAL";

                    string one = _siteCoordInputOne.text.Trim();
                    string two = _siteCoordInputTwo.text.Trim();
                    string three = _siteCoordInputThree.text.Trim();

                    string siteCoordRaw = $"{one},{two},{three}";

                    string centerValue = $"coord@{NAIFID}";

                    URL =
                        $"CENTER={HorizonsAPIParameters.EncodeQuoted(centerValue)}" +
                        $"&COORD_TYPE={HorizonsAPIParameters.EncodeQuoted(coordTypeToken)}" +
                        $"&SITE_COORD={HorizonsAPIParameters.EncodeQuoted(siteCoordRaw)}";

                    return true;
                }

            default:
                return false;
        }
    }

    public void ApplyDefault()
    {
        _inputTypeDropdown.value = 0;
        _inputTypeDropdown.RefreshShownValue();
    }

    public void OnIAUCodesClick() => Application.OpenURL(IAU_CODES_URL);
}
