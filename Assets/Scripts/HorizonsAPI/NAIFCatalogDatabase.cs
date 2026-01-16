using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using UnityEngine;

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
    [SerializeField] private List<BodyCatalog> entries = new();

    // Fast lookup map
    [NonSerialized] private Dictionary<int, int> _idToIndex;

    // Exact lookup maps
    [NonSerialized] private Dictionary<string, List<int>> _nameToIds;
    [NonSerialized] private Dictionary<string, List<int>> _designationToIds;
    [NonSerialized] private Dictionary<string, List<int>> _aliasTokenToIds;

    // SearchText[i] corresponds to entries[i]
    [NonSerialized] private List<string> _searchText;

    // Incremental search caching
    [NonSerialized] private string _lastQuery = string.Empty;
    [NonSerialized] private readonly List<int> _lastCandidateIdxs = new(); // indexes into entries list

    public IReadOnlyList<BodyCatalog> Entries => entries;

    private void OnEnable()
    {
        BuildIndexes();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Editor-time only. Keeps indexes valid while editing.
        if (!Application.isPlaying)
            BuildIndexes();
    }
#endif

    public void BuildIndexes()
    {
        _idToIndex = new Dictionary<int, int>(entries.Count);

        _nameToIds = new Dictionary<string, List<int>>(entries.Count, StringComparer.OrdinalIgnoreCase);
        _designationToIds = new Dictionary<string, List<int>>(entries.Count, StringComparer.OrdinalIgnoreCase);
        _aliasTokenToIds = new Dictionary<string, List<int>>(entries.Count, StringComparer.OrdinalIgnoreCase);

        _searchText = new List<string>(entries.Count);

        for (int i = 0; i < entries.Count; i++)
        {
            var c = entries[i];

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

    static string BuildSearchText(BodyCatalog c)
    {
        return $"{c.NAIFID} {c.Name} {c.Designation} {c.Aliases}".Trim();
    }

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

            yield return text.Substring(start, i - start);
        }
    }

    bool EnsureBuilt()
    {
        if (_idToIndex == null || _searchText == null || _searchText.Count != entries.Count)
        {
            BuildIndexes();
            return true;
        }
        return false;
    }

    public bool TryUpdateCatalog(List<BodyCatalog> newCatalog, out List<BodyCatalog> updatedCatalog, out List<string> changes)
    {
        updatedCatalog = new(Entries);
        changes = new();

        if (newCatalog == null || newCatalog.Count <= 0) return false;

        // Create and build a dictionary of the new catalog and the old catalog (faster comparison)
        Dictionary<int, BodyCatalog> _old = new(Entries.Count);
        Dictionary<int, BodyCatalog> _new = new(newCatalog.Count);
        for (int i = 0; i < Entries.Count; i++)
            if (!_old.TryAdd(Entries[i].NAIFID, Entries[i])) continue;
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
                    changes.Add($"Updated {old.NAIFID} 'Name': '{old.Name}' to '{newName}'");

                if (!string.Equals(newDesignation, oldDesignation, StringComparison.OrdinalIgnoreCase))
                    changes.Add($"Updated {old.NAIFID} 'Designation': '{old.Designation}' to '{newDesignation}'");

                if (!string.Equals(newAlias, oldAlias, StringComparison.OrdinalIgnoreCase))
                    changes.Add($"Updated {old.NAIFID} 'IAU/Alias/Other': '{old.Aliases}' to '{newAlias}'");
            }
        }

        bool changed = changes.Count > 0;

        if (changed) updatedCatalog = new List<BodyCatalog>(newCatalog);
        return changed;
    }

    public void ReplaceCatalog(List<BodyCatalog> newCatalog)
    {
        if (newCatalog == null || newCatalog.Count <= 0)
        {
            Debug.LogError($"Could not replace catalog from {Entries} to {newCatalog}");
            return;
        }

        entries = new List<BodyCatalog>(newCatalog);
    }

    public bool TryGetCatalogById(int id, out BodyCatalog catalog)
    {
        catalog = default;
        EnsureBuilt();

        if (!_idToIndex.TryGetValue(id, out int idx))
            return false;

        catalog = entries[idx];
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
            _lastCandidateIdxs.Capacity = Math.Max(_lastCandidateIdxs.Capacity, entries.Count);

            for (int i = 0; i < entries.Count; i++)
                _lastCandidateIdxs.Add(i);
        }

        int exactIdIdx = -1;
        if (int.TryParse(query, out int exactId) && _idToIndex.TryGetValue(exactId, out int idxExact))
        {
            exactIdIdx = idxExact;
            results.Add(entries[exactIdIdx]);
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
                    results.Add(entries[entryIdx]);
            }
        }

        // Shrink candidates to only matches
        if (write < _lastCandidateIdxs.Count)
            _lastCandidateIdxs.RemoveRange(write, _lastCandidateIdxs.Count - write);

        _lastQuery = query;
    }


    // Runtime Add 
    public bool TryAddCatalogRuntime(BodyCatalog catalogToAdd)
    {
        EnsureBuilt();

        if (_idToIndex.ContainsKey(catalogToAdd.NAIFID))
        {
            Debug.LogWarning($"[NAIFCatalogDatabase] NAIFID already exists: {catalogToAdd.NAIFID}");
            return false;
        }

        entries.Add(catalogToAdd);
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
