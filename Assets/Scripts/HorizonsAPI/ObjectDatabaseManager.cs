using System.Collections.Generic;
using UnityEngine;

public class ObjectDatabaseManager : MonoBehaviour
{
    Dictionary<string, ObjectDataJSON> _objectData = new();

    public int GetEphemerisCount(string objectName) => _objectData.TryGetValue(objectName, out var d) ? d.EphemerisData.Count : 0;

    public int GetPhysicalTraitsCount(string objectName) => _objectData.TryGetValue(objectName, out var d) ? d.PhysicalTraitsData.Count : 0;

    public IReadOnlyCollection<string> GetAllObjectNames() => _objectData.Keys;

    public void PopulateFromSavedData()
    {
        _objectData.Clear();
        foreach (var kvp in HorizonsResponseSaver.GetAllSavedObjectData())
        {
            _objectData[kvp.Key] = kvp.Value;
        }
    }
}