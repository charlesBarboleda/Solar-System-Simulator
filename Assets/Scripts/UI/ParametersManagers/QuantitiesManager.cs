using TMPro;
using System.Globalization;
using UnityEngine;
using System.Collections.Generic;

public class QuantitiesManager : MonoBehaviour, IAPIParameterManager, IDefaultable, IInputValidation
{
    [Header("General references")]
    [SerializeField] GameObject _paramContainer;

    [Header("Input type references")]
    [SerializeField] TMP_Dropdown _inputTypeDropdown;

    [Header("Preset references")]
    [SerializeField] GameObject _presetInputContainer;
    [SerializeField] TMP_Dropdown _presetDropdown;

    [Header("Custom references")]
    [SerializeField] GameObject _customInputContainer;
    [SerializeField] TMP_InputField _customInputField;
    [SerializeField] GameObject _invalidInput;
    string _normalizedCustomQuantities;

    public void OnCustomInputEdit(string _)
    {
        if (_invalidInput.activeInHierarchy) _invalidInput.SetActive(false);
    }

    public void OnInputTypeChange(int idx)
    {
        InputType inputType = (InputType)idx;

        switch (inputType)
        {
            case InputType.Presets:
                _presetInputContainer.SetActive(true);

                _customInputContainer.SetActive(false);
                return;
            case InputType.Custom:
                _customInputContainer.SetActive(true);

                _presetInputContainer.SetActive(false);
                return;
        }
    }
    public void ApplyDefault()
    {
        _inputTypeDropdown.value = 0;
        _inputTypeDropdown.RefreshShownValue();
        OnInputTypeChange(_inputTypeDropdown.value);

        _presetDropdown.value = 0;
        _presetDropdown.RefreshShownValue();

        _invalidInput.SetActive(false);
        _normalizedCustomQuantities = string.Empty;
    }


    public bool TryGetURL(out string URL)
    {
        URL = "QUANTITIES=";
        string value;

        InputType inputType = (InputType)_inputTypeDropdown.value;
        switch (inputType)
        {
            case InputType.Presets:
                Presets presetChosen = (Presets)_presetDropdown.value;
                value = HorizonsAPIParameters.EncodeQuoted(presetChosen.ToString());
                URL += value;
                return true;
            case InputType.Custom:
                if (!IsValidInput()) return false;

                value = HorizonsAPIParameters.EncodeQuoted(_normalizedCustomQuantities);
                URL += value;
                return true;
        }

        return false;
    }

    enum Presets
    {
        A,
        B,
        C,
        D,
        E,
        F
    }

    enum InputType
    {
        Presets,
        Custom
    }
    public GameObject GetParameterContainer() => _paramContainer;

    public bool IsValidInput()
    {
        _invalidInput.SetActive(false);
        HashSet<int> seen = new();

        string input = _customInputField.text.Trim();
        if (string.IsNullOrEmpty(input))
        {
            UIMessage.Instance.NewFadingMessage(MessageType.Error, "[Quantities] Invalid input; input is null/empty", 20f);
            _invalidInput.SetActive(true);
            return false;
        }

        string[] tokens = input.Split(',');
        List<string> normalized = new(tokens.Length);

        for (int i = 0; i < tokens.Length; i++)
        {
            string token = tokens[i].Trim();

            if (string.IsNullOrEmpty(token))
            {
                UIMessage.Instance.NewFadingMessage(MessageType.Error, $"[Quantities] Invalid input; empty quantity at position {i + 1}", 20f);
                _invalidInput.SetActive(true);
                return false;
            }

            if (!int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out int q))
            {
                UIMessage.Instance.NewFadingMessage(MessageType.Error, $"[Quantities] Invalid input; could not parse quantity '{token}'", 20f);
                _invalidInput.SetActive(true);
                return false;
            }

            if (q < 1 || q > 49)
            {
                UIMessage.Instance.NewFadingMessage(MessageType.Error, $"[Quantities] Invalid quantity '{q}'; must be 1..49", 20f);
                _invalidInput.SetActive(true);
                return false;
            }

            if (!seen.Add(q)) continue;

            normalized.Add(q.ToString(CultureInfo.InvariantCulture));
        }

        _normalizedCustomQuantities = string.Join(",", normalized);
        return true;
    }
}
