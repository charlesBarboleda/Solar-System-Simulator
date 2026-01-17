using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

[Serializable]
public class BodyCatalogListWrapper
{
    public List<BodyCatalog> Database;
}

[Serializable]
public struct BodyCatalog
{
    public int NAIFID;
    public string Name;
    public string Designation;
    public string Aliases;
}

[CreateAssetMenu(menuName = "Horizons/NAIF Bodies Catalog")]
public class NAIFCatalogDatabase : ScriptableObject
{
    [SerializeField] List<BodyCatalog> _catalogDatabase = new();

    // Fast lookup map
    [NonSerialized] Dictionary<int, int> _idToIndex;

    // Exact lookup maps
    [NonSerialized] Dictionary<string, List<int>> _nameToIds;
    [NonSerialized] Dictionary<string, List<int>> _designationToIds;
    [NonSerialized] Dictionary<string, List<int>> _aliasTokenToIds;

    // SearchText[i] corresponds to _database[i]
    [NonSerialized] List<string> _searchText;

    // Incremental search caching
    [NonSerialized] string _lastQuery = string.Empty;
    [NonSerialized] readonly List<int> _lastCandidateIdxs = new(); // indexes into _database list

    public IReadOnlyList<BodyCatalog> Database => _catalogDatabase;



#if UNITY_EDITOR
    void OnValidate()
    {
        // Editor-time only. Keeps indexes valid while editing.
        if (!Application.isPlaying)
            BuildIndexes();
    }
#endif

    void BuildIndexes()
    {
        _idToIndex = new Dictionary<int, int>(_catalogDatabase.Count);

        _nameToIds = new Dictionary<string, List<int>>(_catalogDatabase.Count, StringComparer.OrdinalIgnoreCase);
        _designationToIds = new Dictionary<string, List<int>>(_catalogDatabase.Count, StringComparer.OrdinalIgnoreCase);
        _aliasTokenToIds = new Dictionary<string, List<int>>(_catalogDatabase.Count, StringComparer.OrdinalIgnoreCase);

        _searchText = new List<string>(_catalogDatabase.Count);

        for (int i = 0; i < _catalogDatabase.Count; i++)
        {
            var c = _catalogDatabase[i];

            _idToIndex[c.NAIFID] = i;

            AddKey(_nameToIds, c.Name, c.NAIFID);
            AddKey(_designationToIds, c.Designation, c.NAIFID);

            foreach (var token in SplitTokens(c.Aliases))
                AddKey(_aliasTokenToIds, token, c.NAIFID);

            _searchText.Add(BuildSearchText(c));
        }

        // Reset incremental search cache
        _lastQuery = string.Empty;
        _lastCandidateIdxs.Clear();
    }

    static string BuildSearchText(BodyCatalog c) => $"{c.NAIFID} {c.Name} {c.Designation} {c.Aliases}".Trim();

    static void AddKey(Dictionary<string, List<int>> map, string key, int id)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        key = key.Trim();

        if (!map.TryGetValue(key, out var list))
        {
            list = new List<int>(1);
            map[key] = list;
        }

        if (!list.Contains(id))
            list.Add(id);
    }

    static IEnumerable<string> SplitTokens(string text)
    {
        // Alias tokens: "JXVIII 2000J1" -> ["JXVIII", "2000J1"]
        if (string.IsNullOrWhiteSpace(text))
            yield break;

        text = text.Trim();

        int i = 0;
        while (i < text.Length)
        {
            while (i < text.Length && char.IsWhiteSpace(text[i])) i++;
            if (i >= text.Length) yield break;

            int start = i;
            while (i < text.Length && !char.IsWhiteSpace(text[i])) i++;

            yield return text[start..i];
        }
    }

    bool EnsureBuilt()
    {
        if (_idToIndex == null || _searchText == null || _searchText.Count != _catalogDatabase.Count)
        {
            BuildIndexes();
            return true;
        }
        return false;
    }

    public bool TryUpdateCatalog(List<BodyCatalog> newCatalog, out List<string> changes)
    {
        changes = new();

        if (newCatalog == null || newCatalog.Count <= 0) return false;

        // Create and build a dictionary of the new catalog and the old catalog (faster comparison)
        Dictionary<int, BodyCatalog> _old = new(_catalogDatabase.Count);
        Dictionary<int, BodyCatalog> _new = new(newCatalog.Count);
        for (int i = 0; i < _catalogDatabase.Count; i++)
            if (!_old.TryAdd(_catalogDatabase[i].NAIFID, _catalogDatabase[i])) continue;
        for (int i = 0; i < newCatalog.Count; i++)
            if (!_new.TryAdd(newCatalog[i].NAIFID, newCatalog[i])) continue;

        foreach (var oldEntry in _old)
            if (!_new.ContainsKey(oldEntry.Key)) changes.Add($"Removed NAIFID: {oldEntry.Key}");

        foreach (var newEntry in _new)
        {
            // if the old catalog does not contain this NAIFID, it means its a new catalog entry entirely
            if (!_old.TryGetValue(newEntry.Key, out BodyCatalog old))
                changes.Add($"New NAIFID Added: {newEntry.Key}");
            else // if the old catalog does contain the naifid, check the other variables 
            {
                string newName = newEntry.Value.Name?.Trim();
                string oldName = old.Name?.Trim();
                string newDesignation = newEntry.Value.Designation?.Trim();
                string oldDesignation = old.Designation?.Trim();
                string newAlias = newEntry.Value.Aliases?.Trim();
                string oldAlias = old.Aliases?.Trim();

                if (!string.Equals(newName, oldName, StringComparison.OrdinalIgnoreCase))
                    changes.Add($"Updated '{old.NAIFID}' Name: '{old.Name}' to '{newName}'");

                if (!string.Equals(newDesignation, oldDesignation, StringComparison.OrdinalIgnoreCase))
                    changes.Add($"Updated '{old.NAIFID}' Designation: '{old.Designation}' to '{newDesignation}'");

                if (!string.Equals(newAlias, oldAlias, StringComparison.OrdinalIgnoreCase))
                    changes.Add($"Updated '{old.NAIFID}' IAU/Alias/Other: '{old.Aliases}' to '{newAlias}'");
            }
        }

        bool changed = changes.Count > 0;

        if (changed)
        {
            List<BodyCatalog> updatedCatalog = new(newCatalog);
            if (!TryReplaceCatalog(updatedCatalog)) return false;
        }

        return changed;
    }

    public bool TryReplaceCatalog(List<BodyCatalog> newCatalog)
    {
        if (newCatalog == null || newCatalog.Count <= 0)
        {
            Debug.LogError($"Could not replace catalog from {_catalogDatabase} to {newCatalog}");
            return false;
        }

        if (!TryStoreCatalogDBAsJSON(newCatalog)) return false;

        _catalogDatabase = new List<BodyCatalog>(newCatalog);
        BuildIndexes();

        return true;
    }

    public bool TryGetCatalogById(int id, out BodyCatalog catalog)
    {
        catalog = default;
        EnsureBuilt();

        if (!_idToIndex.TryGetValue(id, out int idx))
            return false;

        catalog = _catalogDatabase[idx];
        return true;
    }

    public void SearchContains(string query, List<BodyCatalog> results, int limit = 25)
    {
        results.Clear();
        EnsureBuilt();

        if (string.IsNullOrWhiteSpace(query))
        {
            _lastQuery = string.Empty;
            _lastCandidateIdxs.Clear();
            return;
        }

        query = query.Trim();

        bool canFilterPrevious =
            !string.IsNullOrEmpty(_lastQuery) &&
            query.StartsWith(_lastQuery, StringComparison.OrdinalIgnoreCase);

        if (!canFilterPrevious)
        {
            _lastCandidateIdxs.Clear();
            _lastCandidateIdxs.Capacity = Math.Max(_lastCandidateIdxs.Capacity, _catalogDatabase.Count);

            for (int i = 0; i < _catalogDatabase.Count; i++)
                _lastCandidateIdxs.Add(i);
        }

        int exactIdIdx = -1;
        if (int.TryParse(query, out int exactId) && _idToIndex.TryGetValue(exactId, out int idxExact))
        {
            exactIdIdx = idxExact;
            results.Add(_catalogDatabase[exactIdIdx]);
        }

        int write = 0;

        for (int read = 0; read < _lastCandidateIdxs.Count; read++)
        {
            int entryIdx = _lastCandidateIdxs[read];

            if (entryIdx == exactIdIdx)
            {
                _lastCandidateIdxs[write++] = entryIdx;
                continue;
            }

            if (_searchText[entryIdx].IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _lastCandidateIdxs[write++] = entryIdx;

                if (results.Count < limit)
                    results.Add(_catalogDatabase[entryIdx]);
            }
        }

        // Shrink candidates to only matches
        if (write < _lastCandidateIdxs.Count)
            _lastCandidateIdxs.RemoveRange(write, _lastCandidateIdxs.Count - write);

        _lastQuery = query;
    }

    public bool TryStoreCatalogDBAsJSON(List<BodyCatalog> _database)
    {
        if (_database == null || _database.Count <= 0)
        {
            Debug.LogError($"Could not store {_database} as JSON");
            return false;
        }

        var jsonWrapper = new BodyCatalogListWrapper { Database = _database };
        string json = JsonUtility.ToJson(jsonWrapper, prettyPrint: true);
        string folder = Path.Combine(Application.persistentDataPath, "HorizonsNAIFDatabaseCache");
        string filePath = Path.Combine(folder, $"naif_major_bodies_latest.json");
        Directory.CreateDirectory(folder);
        string tempPath = filePath + ".tmp";
        File.WriteAllText(tempPath, json);
        File.Copy(tempPath, filePath, overwrite: true);
        File.Delete(tempPath);
        Debug.Log($"Saved catalog JSON to: {filePath}");
        return true;
    }

    static string GetCacheJSONFolder() => Path.Combine(Application.persistentDataPath, "HorizonsNAIFDatabaseCache");

    static string GetCacheJSONFilePath() => Path.Combine(GetCacheJSONFolder(), "naif_major_bodies_latest.json");

    public bool HasLocalJSONDatabase(out string json)
    {
        json = null;
        string path = GetCacheJSONFilePath();

        if (!File.Exists(path))
            return false;

        json = File.ReadAllText(path);
        return !string.IsNullOrWhiteSpace(json);
    }

    public bool TryLoadCatalogDBFromJSON(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError($"Could not load a NAIF catalog database from '{json}'");
            return false;
        }

        var loaded = JsonUtility.FromJson<BodyCatalogListWrapper>(json);
        if (!TryReplaceCatalog(loaded.Database)) return false;
        return true;
    }

    // Runtime Add 
    public bool TryAddCatalogRuntime(BodyCatalog catalogToAdd)
    {
        EnsureBuilt();

        if (_idToIndex.ContainsKey(catalogToAdd.NAIFID))
        {
            Debug.LogWarning($"[NAIFCatalog_Database] NAIFID already exists: {catalogToAdd.NAIFID}");
            return false;
        }

        _catalogDatabase.Add(catalogToAdd);
        BuildIndexes();
        return true;
    }

    // Exact string to ids lookups 
    public bool TryGetIdsByName(string name, out IReadOnlyList<int> ids) => TryGetIds(_nameToIds, name, out ids);
    public bool TryGetIdsByDesignation(string designation, out IReadOnlyList<int> ids) => TryGetIds(_designationToIds, designation, out ids);
    public bool TryGetIdsByAliasToken(string aliasToken, out IReadOnlyList<int> ids) => TryGetIds(_aliasTokenToIds, aliasToken, out ids);

    static bool TryGetIds(Dictionary<string, List<int>> map, string key, out IReadOnlyList<int> ids)
    {
        ids = Array.Empty<int>();

        if (map == null || string.IsNullOrWhiteSpace(key))
            return false;

        key = key.Trim();

        if (!map.TryGetValue(key, out var list) || list.Count == 0)
            return false;

        ids = list;
        return true;
    }
}
