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
                ChangeInputContents(label: "Value :", placeholder: "e.g. '3'", contentType: TMP_InputField.ContentType.IntegerNumber);

                _unitDropdownContainer.SetActive(false);
                _invalidInputError.SetActive(false);
                return;
            case InputType.AngularStepping:
                _inputFieldValueContainer.SetActive(true);
                ChangeInputContents(label: "Value :", placeholder: "e.g. '3'", contentType: TMP_InputField.ContentType.IntegerNumber);

                _planeDropdownContainer.SetActive(false);
                _unitDropdownContainer.SetActive(false);
                _invalidInputError.SetActive(false);
                return;
        }
    }
    public void ApplyDefault()
    {
        _unitDropdown.value = 0;
        _unitDropdown.RefreshShownValue();
    }

    public bool IsValidInput()
    {
        if (!int.TryParse(_inputFieldValue.text, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
        {
            UIMessage.Instance.NewFadingMessage(MessageType.Error, $"[Step Size] Invalid StepSize input '{_inputFieldValue.text}'", 20f);
            _invalidInputError.SetActive(true);
            return false;
        }

        return true;
    }

    public bool TryGetURL(out string URL)
    {
        URL = "STEP_SIZE=";
        return false;

    }

    public void OnValueInputChange(string _)
    {
        if (_invalidInputError.activeInHierarchy) _invalidInputError.SetActive(false);
        return;
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
