using System.Collections;
using System.Runtime.InteropServices.WindowsRuntime;
using TMPro;
using Unity.AppUI.Core;
using Unity.VisualScripting;
using UnityEngine;

public class TListManager : MonoBehaviour
{
    [Header("TList Type Manager")]
    [SerializeField] TListTypeManager _tListTypeManager;

    [Header("Julian Day References")]
    [SerializeField] TMP_InputField _julianDayInput;
    [SerializeField] TMP_InputField _modifiedJulianDayInput;

    [Header("Viewport Scrollable Content")]
    [SerializeField] GameObject _scrollableContent;
    [SerializeField] GameObject _contentText;

    [Header("Calendar Day References")]
    [SerializeField] TMP_InputField _yearInput;
    [SerializeField] TMP_InputField _monthInput;
    [SerializeField] TMP_InputField _dayInput;
    [SerializeField] TMP_InputField _hourInput;
    [SerializeField] TMP_InputField _minuteInput;
    [SerializeField] TMP_InputField _secondInput;

    [Header("Invalid Input References")]
    [SerializeField] GameObject _yearInputInvalid;
    [SerializeField] GameObject _monthInputInvalid;
    [SerializeField] GameObject _dayInputInvalid;
    [SerializeField] GameObject _hourInputInvalid;
    [SerializeField] GameObject _minuteInputInvalid;
    [SerializeField] GameObject _secondInputInvalid;
    [SerializeField] GameObject _julianDayInvalid;
    [SerializeField] GameObject _mJulianDayInvalid;


    public void AddButton()
    {
        if (_tListTypeManager != null)
        {
            switch (_tListTypeManager.TListTypeDropdown.value)
            {
                // Julian Day
                case 0:
                    if (TryAddJulianDay(isModified: false)) _julianDayInvalid.SetActive(false);
                    break;
                // Modified Julian Day
                case 1:
                    if (TryAddJulianDay(isModified: true)) _mJulianDayInvalid.SetActive(false);
                    break;
                // Calendar
                case 2:
                    if (TryAddCalendarDay())
                    {
                        _yearInputInvalid.SetActive(false);
                        _monthInputInvalid.SetActive(false);
                        _dayInputInvalid.SetActive(false);
                        _hourInputInvalid.SetActive(false);
                        _minuteInputInvalid.SetActive(false);
                        _secondInputInvalid.SetActive(false);
                    }
                    break;
            }
        }
    }

    void HandleParseCalendarSuccess(HorizonsAPIParameters.CalendarParseSuccessReason reason)
    {
        switch (reason)
        {
            case HorizonsAPIParameters.CalendarParseSuccessReason.Year:
                _yearInputInvalid.SetActive(false);
                break;
            case HorizonsAPIParameters.CalendarParseSuccessReason.Month:
                _monthInputInvalid.SetActive(false);
                break;
            case HorizonsAPIParameters.CalendarParseSuccessReason.Day:
                _dayInputInvalid.SetActive(false);
                break;
            case HorizonsAPIParameters.CalendarParseSuccessReason.Hour:
                _hourInputInvalid.SetActive(false);
                break;
            case HorizonsAPIParameters.CalendarParseSuccessReason.Minute:
                _minuteInputInvalid.SetActive(false);
                break;
            case HorizonsAPIParameters.CalendarParseSuccessReason.Second:
                _secondInputInvalid.SetActive(false);
                break;
        }
    }

    void HandleParseCalendarFail(HorizonsAPIParameters.CalendarParseFailReason reason, string year, string month, string day, string hour, string minute, string second)
    {
        switch (reason)
        {
            case HorizonsAPIParameters.CalendarParseFailReason.InvalidYear:
                UIMessage.Instance.NewFadingMessage($"[TList] Invalid Calendar Date Year input '{year}'", 30f);
                _yearInputInvalid.SetActive(true);
                break;
            case HorizonsAPIParameters.CalendarParseFailReason.InvalidMonth:
                UIMessage.Instance.NewFadingMessage($"[TList] Invalid Calendar Date Month input '{month}'", 30f);
                _monthInputInvalid.SetActive(true);
                break;
            case HorizonsAPIParameters.CalendarParseFailReason.InvalidDay:
                UIMessage.Instance.NewFadingMessage($"[TList] Invalid Calendar Date Day input '{day}'", 30f);
                _dayInputInvalid.SetActive(true);
                break;
            case HorizonsAPIParameters.CalendarParseFailReason.InvalidHour:
                UIMessage.Instance.NewFadingMessage($"[TList] Invalid Calendar Date Hour input '{hour}'", 30f);
                _hourInputInvalid.SetActive(true);
                break;
            case HorizonsAPIParameters.CalendarParseFailReason.InvalidMinute:
                UIMessage.Instance.NewFadingMessage($"[TList] Invalid Calendar Date Minute input '{minute}'", 30f);
                _minuteInputInvalid.SetActive(true);
                break;
            case HorizonsAPIParameters.CalendarParseFailReason.InvalidSecond:
                UIMessage.Instance.NewFadingMessage($"[TList] Invalid Calendar Date Year input '{second}'", 30f);
                _secondInputInvalid.SetActive(true);
                break;
            case HorizonsAPIParameters.CalendarParseFailReason.BuildUtcFailed:
                UIMessage.Instance.NewFadingMessage($"[TList] Calendar Date UTC Build failed", 30f);
                break;
        }
    }
    bool TryAddCalendarDay()
    {
        string year = !string.IsNullOrEmpty(_yearInput.text) ? _yearInput.text : string.Empty;
        string month = !string.IsNullOrEmpty(_monthInput.text) ? _monthInput.text : "1";
        string day = !string.IsNullOrEmpty(_dayInput.text) ? _dayInput.text : "1";
        string hour = !string.IsNullOrEmpty(_hourInput.text) ? _hourInput.text : "0";
        string minute = !string.IsNullOrEmpty(_minuteInput.text) ? _minuteInput.text : "0";
        string second = !string.IsNullOrEmpty(_secondInput.text) ? _secondInput.text : "0";

        if (!HorizonsAPIParameters.TryParseCalendarDay(
            year: year,
            month: month,
            day: day,
            hour: hour,
            minute: minute,
            second: second,
            dateTime: out string dateTimeOutput,
            onFail: reason => HandleParseCalendarFail(reason, year, month, day, hour, minute, second),
            onSuccess: reason => HandleParseCalendarSuccess(reason)))
        {
            return false;
        }
        else
        {
            GameObject newDateTimeEntry = Instantiate(_contentText);
            newDateTimeEntry.transform.SetParent(_scrollableContent.transform, worldPositionStays: false);
            newDateTimeEntry.transform.localScale = Vector3.one;

            if (newDateTimeEntry.TryGetComponent(out TextMeshProUGUI textComponent)) textComponent.text = dateTimeOutput;
            else
            {
                Debug.LogWarning($"Could not find a TextMeshProUGUI on {newDateTimeEntry.name}");
                return false;
            }

            return true;
        }
    }

    void HandleParseJulianDayFail(string dayInput, bool isModified = false)
    {
        if (isModified)
        {
            UIMessage.Instance.NewFadingMessage($"[TList] Invalid Modified Julian Day input '{dayInput}'", 30f);
            _mJulianDayInvalid.SetActive(true);
        }
        else
        {
            UIMessage.Instance.NewFadingMessage($"[TList] Invalid Julian Day input '{dayInput}'", 30f);
            _julianDayInvalid.SetActive(true);
        }
    }
    bool TryAddJulianDay(bool isModified = false)
    {
        string inputText = isModified ? _modifiedJulianDayInput.text : _julianDayInput.text;

        if (!HorizonsAPIParameters.TryParseJulianDay(
            julianDay: inputText,
            dateTime: out string dateTimeOutput,
            isModified: isModified,
            onFail: () => HandleParseJulianDayFail(inputText, isModified)))
        {
            return false;
        }
        else
        {
            GameObject newDateTimeEntry = Instantiate(_contentText);
            newDateTimeEntry.transform.SetParent(_scrollableContent.transform);
            newDateTimeEntry.transform.localScale = Vector3.one;
            if (newDateTimeEntry.TryGetComponent(out TextMeshProUGUI textComponent)) textComponent.text = dateTimeOutput;
            else
            {
                Debug.LogWarning($"Could not find a TextMeshProUGUI on {newDateTimeEntry.name}");
                return false;
            }

            return true;
        }
    }
}
