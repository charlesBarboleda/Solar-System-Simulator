using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System;

public class LiveSimulationManager : MonoBehaviour
{
    public static LiveSimulationManager Instance { get; private set; }

    [SerializeField] GameObject _liveSimulationPanel;

    [SerializeField] GameObject _rowParent;
    [SerializeField] GameObject _informationPanel;
    [SerializeField] GameObject _rowPrefab;

    [SerializeField] TMP_InputField _searchField;

    Dictionary<string, LiveSimulationRowContainerManager> _rowEntries = new();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void OnSearchFieldValueChanged(string value)
    {
        foreach (var entry in _rowEntries)
        {
            bool shouldShow = entry.Key.Contains(value, StringComparison.OrdinalIgnoreCase);

            GameObject rowObject = entry.Value.gameObject;

            if (rowObject.activeSelf != shouldShow)
            {
                rowObject.SetActive(shouldShow);
            }
        }
    }

    public void CreateNewEntry(AstronomicalObject astronomicalObject, int rowNumber)
    {
        GameObject row = Instantiate(_rowPrefab, _rowParent.transform);

        if (!row.TryGetComponent(out LiveSimulationRowContainerManager manager))
        {
            Debug.LogError($"[LiveSimulationManager] Row prefab missing LiveSimulationRowContainerManager component.");
            Destroy(row);
            return;
        }

        manager.Initialize(astronomicalObject, rowNumber);
        _rowEntries.Add(astronomicalObject.Data.Body.Name, manager);
    }

    public void RemoveEntry(string bodyName)
    {
        if (_rowEntries.TryGetValue(bodyName, out LiveSimulationRowContainerManager rowManager))
        {
            Destroy(rowManager.gameObject);
            _rowEntries.Remove(bodyName);
            RecalculateRowNumbers();
        }
    }

    public void RecalculateRowNumbers()
    {
        int number = 1;
        foreach (LiveSimulationRowContainerManager rowManager in _rowEntries.Values)
        {
            rowManager.SetRowNumber(number);
            number++;
        }
    }

    public void Initialize(List<AstronomicalObject> astronomicalObjects)
    {
        foreach (LiveSimulationRowContainerManager entry in _rowEntries.Values)
        {
            string objectName = entry.GetAstronomicalObject().Data.Body.Name;
            RemoveEntry(objectName);
        }

        _rowEntries.Clear();

        if (astronomicalObjects.Count > 0)
        {
            for (int i = 0; i < astronomicalObjects.Count; i++)
            {
                CreateNewEntry(astronomicalObjects[i], i + 1);
            }
        }
    }

    public void OnClickCloseButton() => _liveSimulationPanel.SetActive(false);
}
