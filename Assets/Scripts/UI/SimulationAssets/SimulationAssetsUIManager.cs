using UnityEngine;
using NaughtyAttributes;
using System.Collections.Generic;
using TMPro;

public class SimulationAssetsUIManager : MonoBehaviour
{
    public static SimulationAssetsUIManager Instance { get; private set; }
    List<SimulationAssetContainerPrefabManager> _simAssetContainerManagers = new();
    Dictionary<string, SimulationAssetContainerPrefabManager> _cachedNames = new();

    [SerializeField] GameObject _simulationAssetsContainer;

    [SerializeField] GameObject _simulationAssetContainerPrefab;
    [SerializeField] GameObject _rowContainerPrefab;
    [SerializeField] GameObject _rowContainerParent;

    [SerializeField] List<GameObject> _rowContainers = new();

    const int MAX_OBJECTS_PER_ROW = 3;
    GameObject _currentRowContainer;



    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _currentRowContainer = AssignCurrentRowContainer();
    }

    public void BuildUI()
    {
        ClearUI();
        _simAssetContainerManagers.Clear();
        _cachedNames.Clear();
        _currentRowContainer = CreateNewSimulationAssetRow();

        List<Data> database = SimulationAssetDatabaseManager.Instance.SimulationAssetDatabase;

        foreach (Data data in database)
        {
            TryCreateNewSimulationAssetObject(data);
        }
    }

    public void OnSimulationAssetUICloseClick()
    {
        _simulationAssetsContainer.SetActive(false);

        if (MainMenuManager.Instance.IsActive)
        {
            MainMenuManager.Instance.EnableMainMenu(true);
            MainMenuManager.Instance.Enable3DContent(false);
        }

    }
    public void OnSearchFieldInput(string input) => SearchByName(input);

    void SearchByName(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            foreach (SimulationAssetContainerPrefabManager manager in _simAssetContainerManagers)
                EnableUIAsset(manager);

            RepackRows();
            return;
        }

        string lowerInput = input.ToLower();

        foreach (KeyValuePair<string, SimulationAssetContainerPrefabManager> entry in _cachedNames)
        {
            if (entry.Key.ToLower().Contains(lowerInput))
                EnableUIAsset(entry.Value);
            else
                DisableUIAsset(entry.Value);
        }

        RepackRows();
    }

    void RepackRows()
    {
        List<SimulationAssetContainerPrefabManager> visible = new();
        foreach (SimulationAssetContainerPrefabManager manager in _simAssetContainerManagers)
        {
            if (manager.gameObject.activeSelf) visible.Add(manager);
        }

        foreach (SimulationAssetContainerPrefabManager manager in visible) manager.transform.SetParent(null);

        foreach (GameObject row in _rowContainers) row.SetActive(false);

        int rowIndex = 0;
        for (int i = 0; i < visible.Count; i++)
        {
            rowIndex = i / MAX_OBJECTS_PER_ROW;

            while (rowIndex >= _rowContainers.Count) CreateNewSimulationAssetRow();

            GameObject row = _rowContainers[rowIndex];
            row.SetActive(true);
            visible[i].transform.SetParent(row.transform);
        }

        for (int i = rowIndex + 1; i < _rowContainers.Count; i++) _rowContainers[i].SetActive(false);

        _currentRowContainer = rowIndex < _rowContainers.Count ? _rowContainers[rowIndex] : _rowContainers[^1];
    }

    public void DisableUIAsset(SimulationAssetContainerPrefabManager manager)
    {
        manager.DisableAllContainers();
        manager.gameObject.SetActive(false);
    }

    public void EnableUIAsset(SimulationAssetContainerPrefabManager manager)
    {
        manager.gameObject.SetActive(true);
    }

    public void DisableAllContainersExclusive(SimulationAssetContainerPrefabManager exception)
    {
        foreach (SimulationAssetContainerPrefabManager manager in _simAssetContainerManagers)
        {
            if (exception.Equals(manager) || !manager.gameObject.activeInHierarchy) continue;

            manager.DisableAllContainers();
        }
    }

    void ClearUI()
    {
        _rowContainers.Clear();

        foreach (Transform child in _rowContainerParent.transform)
        {
            Destroy(child.gameObject);
        }

        _currentRowContainer = null;
    }

    public bool TryCreateNewSimulationAssetObject(Data data)
    {
        int childCount = _currentRowContainer.transform.childCount;

        if (childCount >= MAX_OBJECTS_PER_ROW) _currentRowContainer = CreateNewSimulationAssetRow();

        GameObject go = Instantiate(_simulationAssetContainerPrefab, parent: _currentRowContainer.transform);

        if (!go.TryGetComponent(out SimulationAssetContainerPrefabManager _simulationAssetManager))
        {
            Debug.LogError($"Failed to create a simulation asset object, could not find proper components!");
            return false;
        }

        _simulationAssetManager.InitializeData(data);
        _simAssetContainerManagers.Add(_simulationAssetManager);
        _cachedNames[data.Body.Name] = _simulationAssetManager;

        return true;
    }

    GameObject CreateNewSimulationAssetRow()
    {
        GameObject go = Instantiate(_rowContainerPrefab, _rowContainerParent.transform);
        _currentRowContainer = go;
        _rowContainers.Add(go);

        return go;
    }

    GameObject AssignCurrentRowContainer()
    {
        int count = _rowContainerParent.transform.childCount;

        if (count == 0) return CreateNewSimulationAssetRow();

        return _rowContainerParent.transform.GetChild(count - 1).gameObject;
    }
}
