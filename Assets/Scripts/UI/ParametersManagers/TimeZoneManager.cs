using TMPro;
using UnityEngine;
using System.Globalization;

public class TimeZoneManager : MonoBehaviour, IAPIParameterManager, IInputValidation
{
    [SerializeField] GameObject _parameterContainer;
    public GameObject ParameterContainer => _parameterContainer;

    [SerializeField] TMP_InputField _timeZoneInputField;
    [SerializeField] GameObject _invalidInput;

    public bool IsValidInput()
    {
        _invalidInput.SetActive(false);

        string input = (_timeZoneInputField.text ?? string.Empty).Trim();

        if (input[0] != '+' && input[0] != '-')
        {
            UIMessage.Instance.NewFadingMessage(MessageType.Error, $"[Time Zone] Invalid input format '{input}'", 20f);
            _invalidInput.SetActive(true);
            return false;
        }
        if (input[3] != ':')
        {
            UIMessage.Instance.NewFadingMessage(MessageType.Error, $"[Time Zone] Invalid input format '{input}'", 20f);
            _invalidInput.SetActive(true);
            return false;
        }
        if (!char.IsDigit(input[1]) || !char.IsDigit(input[2]) || !char.IsDigit(input[4]) || !char.IsDigit(input[5]))
        {
            UIMessage.Instance.NewFadingMessage(MessageType.Error, $"[Time Zone] Invalid input format '{input}'", 20f);
            _invalidInput.SetActive(true);
            return false;
        }

        if (!int.TryParse(input.Substring(1, 2), NumberStyles.None, CultureInfo.InvariantCulture, out int hh))
        {
            UIMessage.Instance.NewFadingMessage(MessageType.Error, $"[Time Zone] Invalid input '{input}'", 20f);
            _invalidInput.SetActive(true);
            return false;
        }
        if (!int.TryParse(input.Substring(4, 2), NumberStyles.None, CultureInfo.InvariantCulture, out int mm))
        {
            UIMessage.Instance.NewFadingMessage(MessageType.Error, $"[Time Zone] Invalid input '{input}'", 20f);
            _invalidInput.SetActive(true);
            return false;
        }

        if (hh < 0 || hh > 14)
        {
            UIMessage.Instance.NewFadingMessage(MessageType.Error, $"[Time Zone] Invalid input '{input}'", 20f);
            _invalidInput.SetActive(true);
            return false;
        }
        if (mm < 0 || mm > 59)
        {
            UIMessage.Instance.NewFadingMessage(MessageType.Error, $"[Time Zone] Invalid input '{input}'", 20f);
            _invalidInput.SetActive(true);
            return false;
        }
        if (hh == 14 && mm != 0)
        {
            UIMessage.Instance.NewFadingMessage(MessageType.Error, $"[Time Zone] Invalid input '{input}'", 20f);
            _invalidInput.SetActive(true);
            return false;
        }

        if (input.Length != 6)
        {
            UIMessage.Instance.NewFadingMessage(MessageType.Error, $"[Time Zone] Invalid input format '{input}'", 20f);
            _invalidInput.SetActive(true);
            return false;
        }

        return true;
    }

    public bool TryGetURL(out string URL)
    {
        URL = $"TIME_ZONE=";

        if (!IsValidInput()) return false;

        string offset = _timeZoneInputField.text.Trim();

        URL += $"{HorizonsAPIParameters.EncodeQuoted(offset)}";
        return true;
    }

    public void OnInputEdit(string _)
    {
        if (_invalidInput.activeInHierarchy) _invalidInput.SetActive(false);
        return;
    }
}
