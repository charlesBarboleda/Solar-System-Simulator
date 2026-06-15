using UnityEngine;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;

public class ApplyVectorManager : MonoBehaviour
{
    public static ApplyVectorManager Instance { get; private set; }

    [SerializeField] GameObject _applyVectorPanel;
    [SerializeField] GameObject _applyVectorRowPrefab;
    [SerializeField] GameObject _rowContainerParent;
    [SerializeField] GameObject _relativeToContainer;
    [SerializeField] TMP_Dropdown _relativeToDropdown;
    [SerializeField] TMP_InputField _searchFieldInput;

    [SerializeField] TextMeshProUGUI _headerText;

    [SerializeField] List<ApplyVectorToRowManager> _rowManagers = new();

    Dictionary<string, ApplyVectorToRowManager> _cachedNames = new();

    ApplyVectorMode _currentMode;
    double3 _vectorToApply;

    public enum ApplyVectorMode
    {
        Position,
        Velocity
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void OnSearchFieldInput(string input) => SearchByName(input);

    void SearchByName(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            foreach (ApplyVectorToRowManager manager in _rowManagers)
            {
                manager.gameObject.SetActive(true);
            }
            return;
        }

        string lowerInput = input.ToLower();

        foreach (KeyValuePair<string, ApplyVectorToRowManager> entry in _cachedNames)
        {
            if (entry.Key.ToLower().Contains(lowerInput)) entry.Value.gameObject.SetActive(true);
            else entry.Value.gameObject.SetActive(false);
        }
    }

    public SimulationObject GetRelativeToObject()
    {
        string selectedOption = _relativeToDropdown.options[_relativeToDropdown.value].text;

        if (selectedOption == "Player") return MovementController.Instance;

        if (NBodyManager.Instance.TryGetAstroObjectByName(selectedOption, out AstronomicalObject astroObject))
        {
            return astroObject;
        }

        return null;
    }

    public void OpenPanel(ApplyVectorMode mode, double3 vector)
    {
        _vectorToApply = vector;
        SetVectorMode(mode);

        if (mode == ApplyVectorMode.Position) _relativeToContainer.SetActive(true);
        else _relativeToContainer.SetActive(false);

        foreach (ApplyVectorToRowManager manager in _rowManagers)
        {
            manager.SetVectorToApply(manager.AstronomicalObject, vector, mode);
        }
    }

    void SetVectorMode(ApplyVectorMode mode)
    {
        _applyVectorPanel.SetActive(true);
        _headerText.text = $"Apply {mode} To";

        if (_currentMode != mode) _currentMode = mode;
    }

    public void DeleteRowEntry(string objectName)
    {
        ApplyVectorToRowManager manager = _rowManagers.Find(m => m.ObjectName == objectName);

        if (manager != null)
        {
            _rowManagers.Remove(manager);
            _cachedNames.Remove(objectName);
            Destroy(manager.gameObject);
            ReInitializeRowNumbers();
        }
    }

    public void InitializeList()
    {
        if (NBodyManager.Instance == null) return;

        _rowManagers.Clear();
        _cachedNames.Clear();

        foreach (Transform child in _rowContainerParent.transform) Destroy(child.gameObject);

        List<AstronomicalObject> astronomicalObjects = NBodyManager.Instance.SystemBodies;

        for (int i = 0; i < astronomicalObjects.Count; i++)
        {
            AstronomicalObject astronomicalObject = astronomicalObjects[i];
            CreateNewRowEntry(astronomicalObject, i + 1);
        }
    }

    void ReInitializeRowNumbers()
    {
        for (int i = 0; i < _rowManagers.Count; i++)
        {
            _rowManagers[i].SetRowNumber(i + 1);
        }
    }

    public void PopulateDropdown()
    {
        _relativeToDropdown.ClearOptions();

        List<string> options = new()
        {
            "Player"
        };

        if (NBodyManager.Instance == null || NBodyManager.Instance.SystemBodies.Count == 0)
        {
            _relativeToDropdown.AddOptions(options);
            _relativeToDropdown.RefreshShownValue();
            return;
        }
        else
        {
            foreach (AstronomicalObject astroObject in NBodyManager.Instance.SystemBodies)
            {
                options.Add(astroObject.Data.Body.Name);
            }

            _relativeToDropdown.AddOptions(options);
            _relativeToDropdown.RefreshShownValue();
            return;
        }
    }

    public void OnCloseButtonClick() => _applyVectorPanel.SetActive(false);

    public GameObject CreateNewRowEntry(AstronomicalObject astronomicalObject, int rowNumber)
    {
        GameObject go = Instantiate(_applyVectorRowPrefab, parent: _rowContainerParent.transform);

        if (!go.TryGetComponent(out ApplyVectorToRowManager manager))
        {
            Debug.LogError($"Failed to get ApplyVectorToRowManager component from instantiated prefab: {go.name}");
            Destroy(go);
            return null;
        }

        manager.Initialize(astronomicalObject.Data.Body.Name, rowNumber, astronomicalObject.Data.Display.DisplayImage);
        _rowManagers.Add(manager);
        _cachedNames[astronomicalObject.Data.Body.Name] = manager;
        return go;
    }
}
