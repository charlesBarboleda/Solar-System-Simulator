using TMPro;
using UnityEngine;

public class TListTypeManager : MonoBehaviour
{
    [Header("TList Type reference")]
    public TMP_Dropdown TListTypeDropdown;

    [Header("TList content container references")]
    [SerializeField] GameObject _julianDayContainer;
    [SerializeField] GameObject _mJulianDayContainer;
    [SerializeField] GameObject _calendarYearContainer;
    [SerializeField] GameObject _calendarMonthContainer;
    [SerializeField] GameObject _calendarDayContainer;
    [SerializeField] GameObject _calendarHourContainer;
    [SerializeField] GameObject _calendarMinuteContainer;
    [SerializeField] GameObject _calendarSecondContainer;

    // Julian Day = 0, M-Julian Day = 1, Calendar = 2
    public void OnValueChanged(int idx)
    {
        switch (idx)
        {
            case 0:
                _julianDayContainer.SetActive(true);
                _mJulianDayContainer.SetActive(false);
                _calendarYearContainer.SetActive(false);
                _calendarMonthContainer.SetActive(false);
                _calendarDayContainer.SetActive(false);
                _calendarHourContainer.SetActive(false);
                _calendarMinuteContainer.SetActive(false);
                _calendarSecondContainer.SetActive(false);
                break;
            case 1:
                _mJulianDayContainer.SetActive(true);
                _julianDayContainer.SetActive(false);
                _calendarYearContainer.SetActive(false);
                _calendarMonthContainer.SetActive(false);
                _calendarDayContainer.SetActive(false);
                _calendarHourContainer.SetActive(false);
                _calendarMinuteContainer.SetActive(false);
                _calendarSecondContainer.SetActive(false);
                break;
            case 2:
                _calendarYearContainer.SetActive(true);
                _calendarMonthContainer.SetActive(true);
                _calendarDayContainer.SetActive(true);
                _calendarHourContainer.SetActive(true);
                _calendarMinuteContainer.SetActive(true);
                _calendarSecondContainer.SetActive(true);
                _mJulianDayContainer.SetActive(false);
                _julianDayContainer.SetActive(false);
                break;
            default:
                break;
        }
    }
}
