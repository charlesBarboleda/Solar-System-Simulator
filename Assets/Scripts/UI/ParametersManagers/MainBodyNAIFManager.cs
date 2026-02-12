using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class MainBodyNAIFManager : MonoBehaviour, IAPIParameterManager, IInputValidation, IDefaultable
{
    [SerializeField] TMP_InputField _mainBodyNAIFInput;
    [SerializeField] TextMeshProUGUI _inputLabelText;
    [SerializeField] TextMeshProUGUI _inputPlaceholderText;
    [SerializeField] GameObject _invalidInput;
    [SerializeField] TMP_Dropdown _naifInputTypeDropdown;
    [SerializeField] HorizonsTabsManager _horizonsTabManager;

    [SerializeField] GameObject _parameterContainer;
    public GameObject ParameterContainer => _parameterContainer;

    public void OnInputTypeChange(int valueChanged) => ChangeInputField(valueChanged);
    public void OnNAIFDatabaseClick() => NAIFDatabaseUIController.Instance.OpenPanel(sortOrder: 2);


    enum InputType
    {
        ID,
        Name
    }

    public void OnInputValueChange(string valueChanged)
    {
        if (_invalidInput.activeInHierarchy)
        {
            _invalidInput.SetActive(false);
            return;
        }
    }

    void ChangeInputField(int idx)
    {
        InputType inputType = (InputType)idx;
        switch (inputType)
        {
            case InputType.ID:
                // SetText avoids some allocations vs 'text='. Not that important, but try to use it from now on
                _inputPlaceholderText.SetText("e.g. '300'");
                _inputLabelText.text = "NAIF ID :";
                _mainBodyNAIFInput.contentType = TMP_InputField.ContentType.IntegerNumber;
                _mainBodyNAIFInput.text = string.Empty;
                break;
            case InputType.Name:
                _inputPlaceholderText.SetText("e.g. 'Earth'");
                _inputLabelText.text = "NAIF Name :";
                _mainBodyNAIFInput.contentType = TMP_InputField.ContentType.Alphanumeric;
                _mainBodyNAIFInput.text = string.Empty;
                break;
        }
    }

    public bool TryGetURL(out string URL)
    {
        URL = "COMMAND=";
        _invalidInput.SetActive(false);
        string input = _mainBodyNAIFInput.text.Trim();

        if (!IsValidInput())
        {
            UIMessage.Instance.NewFadingMessage(MessageType.Error, $"[Main Body NAIF] Invalid input; Input cannot be empty", 20f);
            _invalidInput.SetActive(true);
            return false;
        }

        input = Uri.EscapeDataString(input);
        URL += $"{input}";
        return true;
    }

    public bool IsValidInput()
    {
        _invalidInput.SetActive(false);
        string input = _mainBodyNAIFInput.text.Trim();
        if (input == string.Empty || string.IsNullOrEmpty(input)) return false;

        return true;
    }

    public void ApplyDefault()
    {
        _naifInputTypeDropdown.value = 0;
        _naifInputTypeDropdown.RefreshShownValue();
    }

    public GameObject GetParameterContainer() => _parameterContainer;
}
