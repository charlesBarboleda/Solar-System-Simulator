using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct BodyCatalog
{
    public int NAIFID;
    public string Name;
    public string Designation;
    public string Aliases;
}


public class NAIFCatalogQueryManager
{
    // Run-time Query Database
    public List<BodyCatalog> RuntimeCatalogDatabase = new();

    // Fast lookup map
    Dictionary<int, int> _idToIndex;

    // Exact lookup maps
    Dictionary<string, List<int>> _nameToIds;
    Dictionary<string, List<int>> _designationToIds;
    Dictionary<string, List<int>> _aliasTokenToIds;

    // SearchText[i] corresponds to _database[i]
    List<string> _searchText;

    // Incremental search caching
    string _lastQuery = string.Empty;
    readonly List<int> _lastCandidateIdxs = new(); // indexes into _database list

    public IReadOnlyList<BodyCatalog> QueryCatalogDB => RuntimeCatalogDatabase;

    void BuildIndexes()
    {
        _idToIndex = new Dictionary<int, int>(RuntimeCatalogDatabase.Count);

        _nameToIds = new Dictionary<string, List<int>>(RuntimeCatalogDatabase.Count, StringComparer.OrdinalIgnoreCase);
        _designationToIds = new Dictionary<string, List<int>>(RuntimeCatalogDatabase.Count, StringComparer.OrdinalIgnoreCase);
        _aliasTokenToIds = new Dictionary<string, List<int>>(RuntimeCatalogDatabase.Count, StringComparer.OrdinalIgnoreCase);

        _searchText = new List<string>(RuntimeCatalogDatabase.Count);

        for (int i = 0; i < RuntimeCatalogDatabase.Count; i++)
        {
            var c = RuntimeCatalogDatabase[i];

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

    public bool EnsureBuilt()
    {
        if (_idToIndex == null || _searchText == null || _searchText.Count != RuntimeCatalogDatabase.Count)
        {
            BuildIndexes();
            return true;
        }
        return false;
    }

    public bool TrySetQueryCatalog(List<BodyCatalog> catalog)
    {
        if (catalog == null)
        {
            Debug.LogError("Could not set runtime catalog: list was null.");
            return false;
        }

        RuntimeCatalogDatabase = new List<BodyCatalog>(catalog);
        BuildIndexes();
        return true;
    }

    public bool TryGetCatalogById(int id, out BodyCatalog catalog)
    {
        catalog = default;
        EnsureBuilt();

        if (!_idToIndex.TryGetValue(id, out int idx))
            return false;

        catalog = RuntimeCatalogDatabase[idx];
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
            _lastCandidateIdxs.Capacity = Math.Max(_lastCandidateIdxs.Capacity, RuntimeCatalogDatabase.Count);

            for (int i = 0; i < RuntimeCatalogDatabase.Count; i++)
                _lastCandidateIdxs.Add(i);
        }

        int exactIdIdx = -1;
        if (int.TryParse(query, out int exactId) && _idToIndex.TryGetValue(exactId, out int idxExact))
        {
            exactIdIdx = idxExact;
            results.Add(RuntimeCatalogDatabase[exactIdIdx]);
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
                    results.Add(RuntimeCatalogDatabase[entryIdx]);
            }
        }

        // Shrink candidates to only matches
        if (write < _lastCandidateIdxs.Count)
            _lastCandidateIdxs.RemoveRange(write, _lastCandidateIdxs.Count - write);

        _lastQuery = query;
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
