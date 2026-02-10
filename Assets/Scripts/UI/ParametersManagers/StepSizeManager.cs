using TMPro;
using UnityEngine;
using System.Globalization;

public class StepSizeManager : MonoBehaviour, IAPIParameterManager, IInputValidation, IDefaultable
{
    [Header("Input Type references")]
    [SerializeField] TMP_Dropdown _inputTypeDropdown;

    [Header("Input references")]
    [SerializeField] TMP_Dropdown _unitDropdown;
    [SerializeField] GameObject _unitDropdownContainer;

    [SerializeField] TextMeshProUGUI _inputLabelText;

    [SerializeField] TextMeshProUGUI _inputPlaceholderText;
    [SerializeField] TMP_InputField _inputFieldValue;
    [SerializeField] GameObject _inputFieldValueContainer;

    [SerializeField] TMP_Dropdown _planeDropdown;
    [SerializeField] GameObject _planeDropdownContainer;

    [SerializeField] GameObject _invalidInputError;

    void ChangeInputContents(string label, string placeholder, TMP_InputField.ContentType contentType)
    {
        _inputLabelText.SetText(label);
        _inputPlaceholderText.SetText(placeholder);
        _inputFieldValue.contentType = contentType;
        _inputFieldValue.ForceLabelUpdate();
    }

    public void OnInputTypeChange(int idx)
    {
        InputType inputType = (InputType)idx;
        switch (inputType)
        {
            case InputType.TimeBased:
                _unitDropdownContainer.SetActive(true);
                _inputFieldValueContainer.SetActive(true);
                ChangeInputContents(label: "Value :", placeholder: "e.g. '3'", contentType: TMP_InputField.ContentType.IntegerNumber);

                _planeDropdownContainer.SetActive(false);
                _invalidInputError.SetActive(false);
                return;

            case InputType.Unitless:
                _inputFieldValueContainer.SetActive(true);
                ChangeInputContents(label: "Value :", placeholder: "e.g. '22'", contentType: TMP_InputField.ContentType.IntegerNumber);

                _unitDropdownContainer.SetActive(false);
                _planeDropdownContainer.SetActive(false);
                _invalidInputError.SetActive(false);
                return;

            case InputType.RiseTransitSet:
                _planeDropdownContainer.SetActive(true);
                _inputFieldValueContainer.SetActive(true);
                ChangeInputContents(label: "Value :", placeholder: "e.g. '3' (minutes)", contentType: TMP_InputField.ContentType.IntegerNumber);

                _unitDropdownContainer.SetActive(false);
                _invalidInputError.SetActive(false);
                return;

            case InputType.AngularStepping:
                _inputFieldValueContainer.SetActive(true);
                ChangeInputContents(label: "Value :", placeholder: "e.g. '600' (arcsec)", contentType: TMP_InputField.ContentType.IntegerNumber);

                _planeDropdownContainer.SetActive(false);
                _unitDropdownContainer.SetActive(false);
                _invalidInputError.SetActive(false);
                return;
        }
    }

    public void ApplyDefault()
    {
        _inputTypeDropdown.value = 0;
        _inputTypeDropdown.RefreshShownValue();

        _unitDropdown.value = 0;
        _unitDropdown.RefreshShownValue();

        _planeDropdown.value = 0;
        _planeDropdown.RefreshShownValue();

        _invalidInputError.SetActive(false);

        // Ensure UI reflects defaults
        OnInputTypeChange(_inputTypeDropdown.value);
    }

    public bool IsValidInput()
    {
        _invalidInputError.SetActive(false);

        InputType inputType = (InputType)_inputTypeDropdown.value;

        string raw = _inputFieldValue.text.Trim();
        if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n))
        {
            UIMessage.Instance.NewFadingMessage(MessageType.Error, $"[Step Size] Invalid value '{_inputFieldValue.text}'", 20f);
            _invalidInputError.SetActive(true);
            return false;
        }

        if (n <= 0)
        {
            UIMessage.Instance.NewFadingMessage(MessageType.Error, $"[Step Size] Value must be > 0 (input {n})", 20f);
            _invalidInputError.SetActive(true);
            return false;
        }

        switch (inputType)
        {
            case InputType.TimeBased:
                // No further numeric constraints here beyond > 0
                return true;

            case InputType.Unitless:
                // Unitless means “split the span into N steps” — N must be >= 1
                return true;

            case InputType.RiseTransitSet:
                // Restriction: Within integer <= 9 minute resolution
                if (n > 9)
                {
                    UIMessage.Instance.NewFadingMessage(MessageType.Error, $"[Step Size] RTS mode requires minutes <= 9 (input: {n})", 20f);
                    _invalidInputError.SetActive(true);
                    return false;
                }
                return true;

            case InputType.AngularStepping:
                // Horizons: VAR range 60..3600 arcseconds
                if (n < 60 || n > 3600)
                {
                    UIMessage.Instance.NewFadingMessage(MessageType.Error, $"[Step Size] Angular stepping must be 60..3600 arcsec (input: {n})", 20f);
                    _invalidInputError.SetActive(true);
                    return false;
                }
                return true;
        }

        UIMessage.Instance.NewFadingMessage(MessageType.Error, $"[Step Size] Unknown input type", 20f);
        _invalidInputError.SetActive(true);
        return false;
    }

    public bool TryGetURL(out string URL)
    {
        URL = "STEP_SIZE=";

        if (!IsValidInput())
            return false;

        InputType inputType = (InputType)_inputTypeDropdown.value;

        int n = int.Parse(_inputFieldValue.text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture);

        switch (inputType)
        {
            case InputType.TimeBased:
                {
                    Units unit = (Units)_unitDropdown.value;

                    string stepSpec = unit switch
                    {
                        Units.Minutes => $"{n}m",
                        Units.Hours => $"{n}h",
                        Units.Days => $"{n}d",
                        Units.Months => $"{n} mo",
                        Units.Years => $"{n} year",
                        _ => $"{n}m"
                    };

                    URL += HorizonsAPIParameters.EncodeQuoted(stepSpec);
                    return true;
                }

            case InputType.Unitless:
                {
                    URL += HorizonsAPIParameters.EncodeQuoted(n.ToString(CultureInfo.InvariantCulture));
                    return true;
                }

            case InputType.RiseTransitSet:
                {
                    RTSMode mode = (RTSMode)_planeDropdown.value;
                    // Format: "{integer}m {MODE}" e.g. "3m TVH"
                    string stepSpec = $"{n}m {mode.ToString().ToUpperInvariant()}";
                    URL += HorizonsAPIParameters.EncodeQuoted(stepSpec);
                    return true;
                }

            case InputType.AngularStepping:
                {
                    // Format: "VAR {arcsec}" e.g. "VAR 600"
                    string stepSpec = $"VAR {n.ToString(CultureInfo.InvariantCulture)}";
                    URL += HorizonsAPIParameters.EncodeQuoted(stepSpec);
                    return true;
                }
        }

        return false;
    }

    public void OnValueInputChange(string _)
    {
        if (_invalidInputError.activeInHierarchy)
            _invalidInputError.SetActive(false);
    }

    enum Units
    {
        Minutes,
        Hours,
        Days,
        Months,
        Years
    }

    enum InputType
    {
        TimeBased,
        Unitless,
        RiseTransitSet,
        AngularStepping
    }

    enum RTSMode
    {
        TVH,
        GEO,
        RAD
    }
}