using TMPro;
using UnityEngine;

public class CenterBodyManager : MonoBehaviour, IAPIParameterManager, IInputValidation, IDefaultable
{
    [SerializeField] TMP_Dropdown _inputTypeDropdown;
    [Header("SiteName / IAU Code references")]
    [SerializeField] TextMeshProUGUI _inputLabel;
    [SerializeField] TMP_InputField _inputField;
    [SerializeField] TextMeshProUGUI _inputPlaceholderText;

    [Header("Container object references")]
    [SerializeField] GameObject _inputContainer;
    [SerializeField] GameObject _coordinateTypeContainer;
    [SerializeField] GameObject _siteCoordinateContainer;
    [SerializeField] GameObject _nameIAUContainer;
    [SerializeField] GameObject _IAUCodesButton;

    [Header("Site Coordinate references")]
    [SerializeField] TMP_Dropdown _coordinateTypeDropdown;
    [SerializeField] TMP_InputField _siteCoordinateInput;
    [SerializeField] TextMeshProUGUI _siteCoordinateInputPlaceholderText;

    const string IAU_CODES_URL = "https://www.minorplanetcenter.net/iau/lists/ObsCodesF.html";

    public void OnCoordinateTypeChange(int idx)
    {
        switch (idx)
        {
            // Geodetic
            case 0:
                _siteCoordinateInputPlaceholderText.SetText("e.g. '120.0000,-33.9000,0.050'");
                _siteCoordinateInputPlaceholderText.fontSize = 10;
                break;
            case 1:
                _siteCoordinateInputPlaceholderText.SetText("e.g. '357.260068,4755.22874,4238.09323'");
                _siteCoordinateInputPlaceholderText.fontSize = 7.5f;
                break;
        }
    }

    public void OnInputTypeChange(int idx)
    {
        switch (idx)
        {
            // DEFAULT: Geocenter
            case 0:
                SetExclusive(InputType.Geocenter);
                break;
            // Site Name
            case 1:
                SetExclusive(InputType.SiteName);
                break;
            // IAU Site Code
            case 2:
                SetExclusive(InputType.IAU);
                break;
            // Site Coordinate
            case 3:
                SetExclusive(InputType.SiteCoordinate);
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
                _siteCoordinateContainer.SetActive(false);
                _nameIAUContainer.SetActive(false);
                _IAUCodesButton.SetActive(false);
                _inputContainer.SetActive(false);
                break;
            case InputType.SiteName:
                _inputContainer.SetActive(true);
                _nameIAUContainer.SetActive(true);
                SwitchInputContents("Site Name :", "e.g. 'Mauna Kea'");
                _siteCoordinateContainer.SetActive(false);
                _coordinateTypeContainer.SetActive(false);
                break;
            case InputType.IAU:
                _inputContainer.SetActive(true);
                _IAUCodesButton.SetActive(true);
                _nameIAUContainer.SetActive(true);
                SwitchInputContents("IAU Code :", "e.g. 'W84'");
                break;
            case InputType.SiteCoordinate:
                _inputContainer.SetActive(true);
                _siteCoordinateContainer.SetActive(true);
                _coordinateTypeContainer.SetActive(true);
                _IAUCodesButton.SetActive(false);
                _nameIAUContainer.SetActive(false);
                break;
        }
    }

    void SwitchInputContents(string label, string placeholder)
    {
        _inputLabel.SetText(label);
        _inputPlaceholderText.SetText(placeholder);
    }

    public bool IsValidInput()
    {
        string input = _inputField.text.Trim();
        if (string.IsNullOrEmpty(input)) return false;
        return true;
    }

    public bool TryGetURL(out string URL)
    {
        URL = string.Empty;

        switch (_inputTypeDropdown.value)
        {
            // Geocenter
            case 0:
                break;
        }
        return true;
    }

    public void ApplyDefault()
    {
        _inputTypeDropdown.value = 0;
        _inputTypeDropdown.RefreshShownValue();
    }

    public void OnIAUCodesClick() => Application.OpenURL(IAU_CODES_URL);


}
