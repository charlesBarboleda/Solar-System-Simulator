using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEngine;
using NaughtyAttributes;
using System.Linq;

public class TListManager : MonoBehaviour, IAPIParameterManager, IDefaultable
{
    [SerializeField] GameObject _parameterContainer;
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

    public GameObject GetParameterContainer() => _parameterContainer;

    [Button]
    public void GetURLTest()
    {
        if (TryGetURL(out string URL))
        {
            Debug.Log($"URL: {URL}");
        }
    }
    public void AddButton()
    {
        if (_tListTypeManager == null) return;

        TListTypeManager.TListInputTypes inputType = (TListTypeManager.TListInputTypes)_tListTypeManager.TListTypeDropdown.value;

        switch (inputType)
        {
            case TListTypeManager.TListInputTypes.Julian:
                if (TryAddAstronomicalDate(isModified: false)) _julianDayInvalid.SetActive(false);
                break;
            case TListTypeManager.TListInputTypes.ModifiedJulian:
                if (TryAddAstronomicalDate(isModified: true)) _mJulianDayInvalid.SetActive(false);
                break;
            case TListTypeManager.TListInputTypes.Calendar:
                if (TryAddCalendarDay()) ClearCalendarValidation();
                break;
        }
    }

    void ClearCalendarValidation()
    {
        _yearInputInvalid.SetActive(false);
        _monthInputInvalid.SetActive(false);
        _dayInputInvalid.SetActive(false);
        _hourInputInvalid.SetActive(false);
        _minuteInputInvalid.SetActive(false);
        _secondInputInvalid.SetActive(false);
    }

    bool TryAddCalendarDay()
    {
        string y = !string.IsNullOrEmpty(_yearInput.text) ? _yearInput.text : "2000";
        string m = !string.IsNullOrEmpty(_monthInput.text) ? _monthInput.text : "1";
        string d = !string.IsNullOrEmpty(_dayInput.text) ? _dayInput.text : "1";
        string h = !string.IsNullOrEmpty(_hourInput.text) ? _hourInput.text : "0";
        string min = !string.IsNullOrEmpty(_minuteInput.text) ? _minuteInput.text : "0";
        string s = !string.IsNullOrEmpty(_secondInput.text) ? _secondInput.text : "0";

        if (!HorizonsAPIParameters.TryParseCalendarDay(year: y, month: m, day: d, hour: h, minute: min, second: s,
            dateTime: out string _,
            onFail: reason => HandleParseCalendarFail(reason, y, m, d, h, min, s),
            onSuccess: reason => HandleParseCalendarSuccess(reason)))
        {
            return false;
        }

        if (int.TryParse(y, out int yearInt))
        {
            y = yearInt < 0 ? $"-{Mathf.Abs(yearInt):D4}" : $"{yearInt:D4}";
        }

        string rawCalendarString = $"{y}-{int.Parse(m):D2}-{int.Parse(d):D2} {int.Parse(h):D2}:{int.Parse(min):D2}:{float.Parse(s):00.0}";

        return CreateListEntry(rawCalendarString);
    }

    bool TryAddAstronomicalDate(bool isModified)
    {
        string inputText = isModified ? _modifiedJulianDayInput.text : _julianDayInput.text;

        if (!HorizonsAPIParameters.TryParseJulianDay(inputText, out _, isModified,
            onFail: () => HandleParseJulianDayFail(inputText, isModified)))
        {
            return false;
        }

        string prefix = isModified ? "MJD " : "JD ";
        return CreateListEntry(prefix + inputText.Trim());
    }

    bool CreateListEntry(string entryString)
    {
        if (_addedDatesList.Contains(entryString))
        {
            UIMessage.Instance.NewFadingMessage(MessageType.Error, $"[TList] Duplicate entry: {entryString}", 5f);
            return false;
        }

        GameObject newUIEntry = Instantiate(_contentText, _scrollableContent.transform);
        newUIEntry.transform.localScale = Vector3.one;

        if (newUIEntry.TryGetComponent(out TextMeshProUGUI textComp))
        {
            textComp.text = entryString;
        }

        Button btn = newUIEntry.GetComponentInChildren<Button>();
        if (btn != null) btn.onClick.AddListener(() => OnRemoveButtonClick(newUIEntry));

        _addedDatesList.Add(entryString);
        _addedDatesUI.Add(newUIEntry);

        UIMessage.Instance.NewFadingMessage(MessageType.Success, $"Added: {entryString}", 3f);
        return true;
    }

    public bool TryGetURL(out string URL)
    {
        URL = string.Empty;
        if (_addedDatesList == null || _addedDatesList.Count == 0) return false;

        // Encoding Rules: ' -> %27, Space -> %20, Comma -> %2C
        var formattedParts = _addedDatesList.Select(date => $"%27{date.Replace(" ", "%20")}%27");

        URL = "TLIST=" + string.Join("%2C", formattedParts);
        return true;
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

    public void ApplyDefault()
    {
        _tListTypeManager.TListTypeDropdown.value = 0;
        _tListTypeManager.TListTypeDropdown.RefreshShownValue();

        _tListTypeManager.OnValueChanged(_tListTypeManager.TListTypeDropdown.value);
    }

    public void OnJulianInputEdit()
    {
        if (_julianDayInvalid.activeInHierarchy) _julianDayInvalid.SetActive(false);
        return;
    }

    public void OnMJulianInputEdit()
    {
        if (_mJulianDayInvalid.activeInHierarchy) _mJulianDayInvalid.SetActive(false);
        return;
    }

    public void OnYearInputEdit()
    {
        if (_yearInputInvalid.activeInHierarchy) _yearInputInvalid.SetActive(false);
        return;
    }

    public void OnMonthInputEdit()
    {
        if (_monthInputInvalid.activeInHierarchy) _monthInputInvalid.SetActive(false);
        return;
    }
    public void OnDayInputEdit()
    {
        if (_dayInputInvalid.activeInHierarchy) _dayInputInvalid.SetActive(false);
        return;
    }
    public void OnHourInputEdit()
    {
        if (_hourInputInvalid.activeInHierarchy) _hourInputInvalid.SetActive(false);
        return;
    }
    public void OnMinuteInputEdit()
    {
        if (_minuteInputInvalid.activeInHierarchy) _minuteInputInvalid.SetActive(false);
        return;
    }
    public void OnSecondInputEdit()
    {
        if (_secondInputInvalid.activeInHierarchy) _secondInputInvalid.SetActive(false);
        return;
    }

}
