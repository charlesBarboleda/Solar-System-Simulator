using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SimulationStateDatabaseUIManager : MonoBehaviour
{
    public static SimulationStateDatabaseUIManager Instance { get; private set; }
    [SerializeField] GameObject _rowElementPrefab;
    [SerializeField] Transform _rowElementParent;
    [SerializeField] GameObject _simulationStateDatabaseContainer;
    [SerializeField] GameObject _saveNameInputContainer;
    [SerializeField] TMP_InputField _saveNameInputField;

    public bool IsInitialized { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        IsInitialized = false;

        Initialize();
    }

    public void OnCloseButtonClick()
    {
        _simulationStateDatabaseContainer.SetActive(false);

        if (MainMenuManager.Instance.IsActive) MainMenuManager.Instance.EnableMainMenu(true);
    }

    public void OnSaveSimulationButtonClick()
    {
        Debug.Log("Save Simulation Button Clicked");
        _saveNameInputContainer.SetActive(true);
        Debug.Log("Save Name Input Container Activated");
    }

    public void OnCloseSaveNameInputClick() => _saveNameInputContainer.SetActive(false);

    public void OnSaveClick()
    {
        if (string.IsNullOrWhiteSpace(_saveNameInputField.text))
        {
            UIMessage.Instance.NewUIMessage(MessageType.Error, "Save name cannot be empty.", "Save Failed");
            return;
        }

        UIMessage.Instance.NewUIConfirmation($"Are you sure you want to save the current simulation state as '{_saveNameInputField.text}'?",
            onYes: () =>
            {
                SimulationSaveLoad.Save(_saveNameInputField.text);
                _saveNameInputField.text = string.Empty;
                _saveNameInputContainer.SetActive(false);
                IsInitialized = false;
                Initialize();
            },
            onNo: () => { });
    }

    public void Initialize()
    {
        if (IsInitialized) return;
        IsInitialized = true;

        foreach (Transform child in _rowElementParent) Destroy(child.gameObject);

        List<SaveSlotMeta> allSaves = SimulationSaveLoad.GetAllSaves();
        foreach (SaveSlotMeta metaData in allSaves)
        {
            CreateNewSimulationStateSaveRow(metaData);
        }
    }

    public void SetInitialize(bool isInitialized)
    {
        IsInitialized = isInitialized;
    }

    public void CreateNewSimulationStateSaveRow(SaveSlotMeta metaData)
    {
        GameObject newRow = Instantiate(_rowElementPrefab, _rowElementParent);
        if (newRow.TryGetComponent(out SimulationStateDatabaseElementRowManager manager)) manager.Initialize(metaData);
    }
}
