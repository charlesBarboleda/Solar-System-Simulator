using TMPro;
using UnityEngine;
using System.Globalization;
using System;
public class StartTimeManager : MonoBehaviour, IAPIParameterManager, IInputValidation
{
    [Header("Input Type dropdown reference")]
    [SerializeField] TMP_Dropdown _inputTypeDropdown;
    [Header("Input Fields references")]
    [SerializeField] TMP_InputField _yearInputField;
    [SerializeField] TMP_InputField _monthInputField;
    [SerializeField] TMP_InputField _dayInputField;
    [SerializeField] TMP_InputField _hourInputField;
    [SerializeField] TMP_InputField _minuteInputField;
    [SerializeField] TMP_InputField _secondInputField;
    [SerializeField] TMP_InputField _julianInputField;
    [SerializeField] TMP_InputField _mJulianInputField;

    [Header("Input container objects")]
    [SerializeField] GameObject _yearInputContainer;
    [SerializeField] GameObject _monthInputContainer;
    [SerializeField] GameObject _dayInputContainer;
    [SerializeField] GameObject _hourInputContainer;
    [SerializeField] GameObject _minuteInputContainer;
    [SerializeField] GameObject _secondInputContainer;
    [SerializeField] GameObject _julianInputContainer;
    [SerializeField] GameObject _mJulianInputContainer;

    [Header("Invalid Input error objects")]
    [SerializeField] GameObject _yearInvalidInput;
    [SerializeField] GameObject _monthInvalidInput;
    [SerializeField] GameObject _dayInvalidInput;
    [SerializeField] GameObject _hourInvalidInput;
    [SerializeField] GameObject _minuteInvalidInput;
    [SerializeField] GameObject _secondInvalidInput;
    [SerializeField] GameObject _julianInvalidInput;
    [SerializeField] GameObject _mJulianInvalidInput;

    public void OnInputTypeChange(int idx)
    {
        InputType inputType = (InputType)idx;

        switch (inputType)
        {
            case InputType.Calendar:
                _yearInputContainer.SetActive(true);
                _monthInputContainer.SetActive(true);
                _dayInputContainer.SetActive(true);
                _hourInputContainer.SetActive(true);
                _minuteInputContainer.SetActive(true);
                _secondInputContainer.SetActive(true);

                _julianInputContainer.SetActive(false);
                _mJulianInputContainer.SetActive(false);
                break;
            case InputType.Julian:
                _julianInputContainer.SetActive(true);

                _yearInputContainer.SetActive(false);
                _monthInputContainer.SetActive(false);
                _dayInputContainer.SetActive(false);
                _hourInputContainer.SetActive(false);
                _minuteInputContainer.SetActive(false);
                _secondInputContainer.SetActive(false);
                _mJulianInputContainer.SetActive(false);
                break;
            case InputType.ModifiedJulian:
                _mJulianInputContainer.SetActive(true);

                _yearInputContainer.SetActive(false);
                _monthInputContainer.SetActive(false);
                _dayInputContainer.SetActive(false);
                _hourInputContainer.SetActive(false);
                _minuteInputContainer.SetActive(false);
                _secondInputContainer.SetActive(false);
                _julianInputContainer.SetActive(false);
                break;
        }
    }
    enum InputType
    {
        Calendar,
        Julian,
        ModifiedJulian
    }

    public bool TryGetURL(out string URL)
    {
        URL = "START_TIME=";

        if (!IsValidInput()) return false;

        InputType inputType = (InputType)_inputTypeDropdown.value;

        switch (inputType)
        {
            case InputType.Calendar:
                {
                    int y = int.Parse(_yearInputField.text, NumberStyles.Integer, CultureInfo.InvariantCulture);
                    int m = int.Parse(_monthInputField.text, NumberStyles.Integer, CultureInfo.InvariantCulture);
                    int d = int.Parse(_dayInputField.text, NumberStyles.Integer, CultureInfo.InvariantCulture);
                    int hh = int.Parse(_hourInputField.text, NumberStyles.Integer, CultureInfo.InvariantCulture);
                    int mm = int.Parse(_minuteInputField.text, NumberStyles.Integer, CultureInfo.InvariantCulture);
                    double ss = double.Parse(_secondInputField.text, NumberStyles.Float, CultureInfo.InvariantCulture);

                    if (!HorizonsAPIParameters.TryBuildUTCTime(y, m, d, hh, mm, ss, out string utcTime, stripUTC: true))
                    {
                        UIMessage.Instance.NewFadingMessage(MessageType.Error, $"[Start Time] Calendar date out of range", 20f);
                        return false;
                    }

                    string encoded = HorizonsAPIParameters.EncodeQuoted(utcTime); // pass unquoted inner
                    URL += encoded;
                    return true;
                }

            case InputType.Julian:
                {
                    if (!HorizonsAPIParameters.TryParseJulianDay(_julianInputField.text, out string utcTime, stripUTC: true))
                        return false;

                    string encoded = HorizonsAPIParameters.EncodeQuoted(utcTime);
                    URL += encoded;
                    return true;
                }

            case InputType.ModifiedJulian:
                {
                    if (!HorizonsAPIParameters.TryParseJulianDay(_mJulianInputField.text, out string utcTime, isModified: true, stripUTC: true))
                        return false;

                    string encoded = HorizonsAPIParameters.EncodeQuoted(utcTime);
                    URL += encoded;
                    return true;
                }
        }

        return false;
    }

    public bool IsValidInput()
    {
        _yearInvalidInput.SetActive(false);
        _monthInvalidInput.SetActive(false);
        _dayInvalidInput.SetActive(false);
        _hourInvalidInput.SetActive(false);
        _minuteInvalidInput.SetActive(false);
        _secondInvalidInput.SetActive(false);
        _julianInvalidInput.SetActive(false);
        _mJulianInvalidInput.SetActive(false);

        InputType inputType = (InputType)_inputTypeDropdown.value;
        switch (inputType)
        {
            case InputType.Calendar:
                if (!HorizonsAPIParameters.IsValidYear(_yearInputField.text, out int y))
                {
                    UIMessage.Instance.NewFadingMessage(MessageType.Error, $"[Start Time] Invalid Year input '{_yearInputField.text}'", 20f);
                    _yearInvalidInput.SetActive(true);
                    return false;
                }
                if (!HorizonsAPIParameters.IsValidMonth(_monthInputField.text, out int m))
                {
                    UIMessage.Instance.NewFadingMessage(MessageType.Error, $"[Start Time] Invalid Month input '{_monthInputField.text}'", 20f);
                    _monthInvalidInput.SetActive(true);
                    return false;
                }
                if (!HorizonsAPIParameters.IsValidDay(_dayInputField.text, m, y, out _))
                {
                    UIMessage.Instance.NewFadingMessage(MessageType.Error, $"[Start Time] Invalid Day input '{_dayInputField.text}'", 20f);
                    _dayInvalidInput.SetActive(true);
                    return false;
                }
                if (!HorizonsAPIParameters.IsValidHour(_hourInputField.text, out _))
                {
                    UIMessage.Instance.NewFadingMessage(MessageType.Error, $"[Start Time] Invalid Hour input '{_hourInputField.text}'", 20f);
                    _hourInvalidInput.SetActive(true);
                    return false;
                }
                if (!HorizonsAPIParameters.IsValidMinute(_minuteInputField.text, out _))
                {
                    UIMessage.Instance.NewFadingMessage(MessageType.Error, $"[Start Time] Invalid Minute input '{_minuteInputField.text}'", 20f);
                    _minuteInvalidInput.SetActive(true);
                    return false;
                }
                if (!HorizonsAPIParameters.IsValidSecond(_secondInputField.text, out _))
                {
                    UIMessage.Instance.NewFadingMessage(MessageType.Error, $"[Start Time] Invalid Second input '{_secondInputField.text}'", 20f);
                    _secondInvalidInput.SetActive(true);
                    return false;
                }
                return true;

            case InputType.Julian:
                if (!HorizonsAPIParameters.TryParseJulianDay(_julianInputField.text, out _))
                {
                    UIMessage.Instance.NewFadingMessage(MessageType.Error, $"[Start Time] Invalid Julian input '{_julianInputField.text}'", 20f);
                    _julianInvalidInput.SetActive(true);
                    return false;
                }
                return true;

            case InputType.ModifiedJulian:
                if (!HorizonsAPIParameters.TryParseJulianDay(_mJulianInputField.text, out _, true))
                {
                    UIMessage.Instance.NewFadingMessage(MessageType.Error, $"[Start Time] Invalid Modified Julian input '{_mJulianInputField.text}'", 20f);
                    _mJulianInvalidInput.SetActive(true);
                    return false;
                }
                return true;
        }

        return false;
    }

    public void OnJulianInputChange(string _)
    {
        if (_julianInvalidInput.activeInHierarchy) _julianInvalidInput.SetActive(false);
        return;
    }
    public void OnMJulianInputChange(string _)
    {
        if (_mJulianInvalidInput.activeInHierarchy) _mJulianInvalidInput.SetActive(false);
        return;
    }

    public void OnYearInputChange(string _)
    {
        if (_yearInvalidInput.activeInHierarchy) _yearInvalidInput.SetActive(false);
        return;
    }

    public void OnMonthInputChange(string _)
    {
        if (_monthInvalidInput.activeInHierarchy) _monthInvalidInput.SetActive(false);
        return;
    }
    public void OnDayInputChange(string _)
    {
        if (_dayInvalidInput.activeInHierarchy) _dayInvalidInput.SetActive(false);
        return;
    }
    public void OnHourInputChange(string _)
    {
        if (_hourInvalidInput.activeInHierarchy) _hourInvalidInput.SetActive(false);
        return;
    }
    public void OnMinuteInputChange(string _)
    {
        if (_minuteInvalidInput.activeInHierarchy) _minuteInvalidInput.SetActive(false);
        return;
    }
    public void OnSecondInputChange(string _)
    {
        if (_secondInvalidInput.activeInHierarchy) _secondInvalidInput.SetActive(false);
        return;
    }
}
