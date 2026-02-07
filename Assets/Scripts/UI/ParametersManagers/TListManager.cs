using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEngine;

public class TListManager : MonoBehaviour, IAPIParameterManager
{
    [Header("TList Type Manager")]
    [SerializeField] TListTypeManager _tListTypeManager;

    [Header("Added Dates contents")]
    [SerializeField] List<string> _addedDatesList;
    public List<string> AddedDatesList => _addedDatesList;
    [SerializeField] List<GameObject> _addedDatesUI;


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
                UIMessage.Instance.NewFadingMessage(MessageType.Error, $"[TList] Invalid Calendar Date Year input '{year}'", 20f);
                _yearInputInvalid.SetActive(true);
                break;
            case HorizonsAPIParameters.CalendarParseFailReason.InvalidMonth:
                UIMessage.Instance.NewFadingMessage(MessageType.Error, $"[TList] Invalid Calendar Date Month input '{month}'", 20f);
                _monthInputInvalid.SetActive(true);
                break;
            case HorizonsAPIParameters.CalendarParseFailReason.InvalidDay:
                UIMessage.Instance.NewFadingMessage(MessageType.Error, $"[TList] Invalid Calendar Date Day input '{day}'", 20f);
                _dayInputInvalid.SetActive(true);
                break;
            case HorizonsAPIParameters.CalendarParseFailReason.InvalidHour:
                UIMessage.Instance.NewFadingMessage(MessageType.Error, $"[TList] Invalid Calendar Date Hour input '{hour}'", 20f);
                _hourInputInvalid.SetActive(true);
                break;
            case HorizonsAPIParameters.CalendarParseFailReason.InvalidMinute:
                UIMessage.Instance.NewFadingMessage(MessageType.Error, $"[TList] Invalid Calendar Date Minute input '{minute}'", 20f);
                _minuteInputInvalid.SetActive(true);
                break;
            case HorizonsAPIParameters.CalendarParseFailReason.InvalidSecond:
                UIMessage.Instance.NewFadingMessage(MessageType.Error, $"[TList] Invalid Calendar Date Second input '{second}'", 20f);
                _secondInputInvalid.SetActive(true);
                break;
            case HorizonsAPIParameters.CalendarParseFailReason.BuildUtcFailed:
                UIMessage.Instance.NewFadingMessage(MessageType.Error, $"[TList] Calendar Date UTC Build failed", 20f);
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
            string reformatted = dateTimeOutput[..^6];

            if (_addedDatesList.Contains(reformatted))
            {
                UIMessage.Instance.NewFadingMessage(MessageType.Error, $"[TList] Failed to add entry, duplicate date: '{reformatted}'", 20f);
                return false;
            }

            GameObject newDateTimeEntry = Instantiate(_contentText);
            newDateTimeEntry.transform.SetParent(_scrollableContent.transform, worldPositionStays: false);
            newDateTimeEntry.transform.localScale = Vector3.one;

            if (newDateTimeEntry.TryGetComponent(out TextMeshProUGUI textComponent)) textComponent.text = dateTimeOutput;
            else
            {
                Debug.LogWarning($"[TList] Could not find a TextMeshProUGUI component on {newDateTimeEntry.name}");
                return false;
            }

            Button removeButton = newDateTimeEntry.GetComponentInChildren<Button>();
            if (removeButton != null) removeButton.onClick.AddListener(() => OnRemoveButtonClick(newDateTimeEntry));
            else
            {
                Debug.LogWarning($"[TList] Could not find a Button component on {newDateTimeEntry.name}");
                return false;
            }

            _addedDatesList.Add(reformatted);
            if (!_addedDatesUI.Contains(newDateTimeEntry)) _addedDatesUI.Add(newDateTimeEntry);

            UIMessage.Instance.NewFadingMessage(MessageType.Success, $"[TList] Successfully added TList date: {dateTimeOutput}", 5f);
            return true;
        }
    }

    void HandleParseJulianDayFail(string dayInput, bool isModified = false)
    {
        if (isModified)
        {
            UIMessage.Instance.NewFadingMessage(MessageType.Error, $"[TList] Invalid Modified Julian Day input '{dayInput}'", 20f);
            _mJulianDayInvalid.SetActive(true);
        }
        else
        {
            UIMessage.Instance.NewFadingMessage(MessageType.Error, $"[TList] Invalid Julian Day input '{dayInput}'", 20f);
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
            string reformatted = dateTimeOutput[..^6];

            if (_addedDatesList.Contains(reformatted))
            {
                UIMessage.Instance.NewFadingMessage(MessageType.Error, $"[TList] Failed to add entry, duplicate date: '{reformatted}'", 20f);
                return false;
            }

            GameObject newDateTimeEntry = Instantiate(_contentText);
            newDateTimeEntry.transform.SetParent(_scrollableContent.transform, worldPositionStays: false);
            newDateTimeEntry.transform.localScale = Vector3.one;

            if (newDateTimeEntry.TryGetComponent(out TextMeshProUGUI textComponent)) textComponent.text = dateTimeOutput;
            else
            {
                Debug.LogWarning($"[TList] Could not find a TextMeshProUGUI component on {newDateTimeEntry.name}");
                return false;
            }

            Button removeButton = newDateTimeEntry.GetComponentInChildren<Button>();
            if (removeButton != null) removeButton.onClick.AddListener(() => OnRemoveButtonClick(newDateTimeEntry));
            else
            {
                Debug.LogWarning($"[TList] Could not find a Button component on {newDateTimeEntry.name}");
                return false;
            }

            _addedDatesList.Add(reformatted);
            if (!_addedDatesUI.Contains(newDateTimeEntry)) _addedDatesUI.Add(newDateTimeEntry);

            UIMessage.Instance.NewFadingMessage(MessageType.Success, $"[TList] Successfully added TList date: {dateTimeOutput}", 5f);
            return true;
        }
    }

    void OnRemoveButtonClick(GameObject entryToRemove)
    {
        int idx = _addedDatesUI.IndexOf(entryToRemove);

        if (!TryRemoveEntry(idx)) return;

        Destroy(entryToRemove);
    }

    bool TryRemoveEntry(int idxToRemove)
    {
        if (_addedDatesList == null || _addedDatesList.Count <= 0 || _addedDatesUI == null || _addedDatesUI.Count <= 0)
        {
            Debug.LogError("[TList] _addedDatesList or _addedDatesUI have no entries");
            return false;
        }
        if (_addedDatesList.Count != _addedDatesUI.Count)
        {
            Debug.LogError("[TList] _addedDatesList and _addedDatesUI are out of sync");
            return false;
        }
        if (idxToRemove < 0 || idxToRemove >= _addedDatesList.Count || idxToRemove >= _addedDatesUI.Count) return false;

        _addedDatesList.RemoveAt(idxToRemove);
        _addedDatesUI.RemoveAt(idxToRemove);

        return true;
    }

    public bool TryGetURL(out string URL)
    {
        URL = string.Empty;

        if (_addedDatesList == null || _addedDatesList.Count == 0)
            return false;

        var encodedEntries = new string[_addedDatesList.Count];

        for (int i = 0; i < _addedDatesList.Count; i++)
        {
            string entry = _addedDatesList[i];
            if (string.IsNullOrWhiteSpace(entry)) return false;

            // quotes = %27
            // spaces = %20
            string entryEncoded = entry.Replace(" ", "%20");
            encodedEntries[i] = $"%27{entryEncoded}%27";
        }

        URL = "TLIST=" + string.Join("%20", encodedEntries);
        return true;
    }

}
