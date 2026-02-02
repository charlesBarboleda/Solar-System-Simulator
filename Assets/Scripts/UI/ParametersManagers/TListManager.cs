using TMPro;
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


    public void AddButton()
    {
        if (_tListTypeManager != null)
        {
            switch (_tListTypeManager.TListTypeDropdown.value)
            {
                // Julian Day
                case 0:
                    TryAddJulianDay(isModified: false);
                    break;
                // Modified Julian Day
                case 1:
                    TryAddJulianDay(isModified: true);
                    break;
                // Calendar
                case 2:
                    TryAddCalendarDay();
                    break;

            }
        }
    }

    void TryAddCalendarDay()
    {
        string year = !string.IsNullOrEmpty(_yearInput.text) ? _yearInput.text : string.Empty;
        string month = !string.IsNullOrEmpty(_monthInput.text) ? _monthInput.text : "1";
        string day = !string.IsNullOrEmpty(_dayInput.text) ? _dayInput.text : "1";
        string hour = !string.IsNullOrEmpty(_hourInput.text) ? _hourInput.text : "0";
        string minute = !string.IsNullOrEmpty(_minuteInput.text) ? _minuteInput.text : "0";
        string second = !string.IsNullOrEmpty(_secondInput.text) ? _secondInput.text : "0";

        if (!HorizonsAPIParameters.TryParseCalendarDay(out string dateTimeOutput, year, month, day, hour, minute, second))
        {
            UIMessage.Instance.NewFadingMessage($"Invalid Calendar Day input '{year}-{month}-{day} {hour}:{minute}:{second}'", 30f);
            return;
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
                return;
            }
        }
    }

    void TryAddJulianDay(bool isModified = false)
    {
        string inputText = isModified ? _modifiedJulianDayInput.text : _julianDayInput.text;

        if (!HorizonsAPIParameters.TryParseJulianDay(inputText, out string dateTimeOutput, isModified))
        {
            if (!isModified) UIMessage.Instance.NewFadingMessage($"Invalid Julian Day input '{inputText}'", 30f);
            else UIMessage.Instance.NewFadingMessage($"Invalid Modified Julian Day input '{inputText}'", 30f);
            return;
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
                return;
            }
        }
    }
}
