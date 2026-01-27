using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;

public class NAIFCatalogManager : MonoBehaviour
{
    public NAIFCatalogQueryManager QueryManager = new();
    [SerializeField] NAIFDatabaseUIController _uiDatabaseController;
    [SerializeField] List<BodyCatalog> _userCatalogDB = new();
    [SerializeField] List<BodyCatalog> _horizonsCatalogDB = new();
    [SerializeField] List<BodyCatalog> _runtimeCatalogDB = new();
    public List<BodyCatalog> UserCatalogDB => _userCatalogDB;
    public List<BodyCatalog> HorizonsCatalogDB => _horizonsCatalogDB;
    public List<BodyCatalog> RuntimeCatalogDB => _runtimeCatalogDB;


    [Header("Response")]
    public string RawHorizonCatalogResponse => _rawHorizonCatalogResponse;
    string _rawHorizonCatalogResponse;
    bool _isUpdatingHorizonsDatabase;

    public bool AutoUpdateDatabase;
    bool _didRuntimeDatabaseChange;
    public bool DidDatabaseUpdate => _didRuntimeDatabaseChange;
    List<string> _runtimeDatabaseChanges = new();
    public IReadOnlyList<string> RuntimeDatabaseChanges => _runtimeDatabaseChanges;

    readonly string _majorBodiesCatalogURL = "https://ssd.jpl.nasa.gov/api/horizons.api?format=json&COMMAND='MB'";

    void Awake()
    {
        if (QueryManager == null)
        {
            Debug.LogError($"Could not find a 'NAIFCatalogQueryManager', initializing a new one");
            QueryManager = new();
        }

        if (_uiDatabaseController == null)
        {
            Debug.LogError($"UIDatabaseController is null. NAIFCatalogManager did not initialize");
            enabled = false;
            return;
        }

        if (!TryInitializeCatalogDB())
        {
            Debug.LogError($"Failed to initialize catalog database; Disabling 'NAIFCatalogManager'");
            enabled = false;
            return;
        }
    }

    public void RefreshHorizonsCatalog() => StartCoroutine(UpdateHorizonsDatabase());

    IEnumerator UpdateHorizonsDatabase()
    {
        if (_isUpdatingHorizonsDatabase) yield break;
        _isUpdatingHorizonsDatabase = true;

        if (QueryManager == null)
        {
            Debug.LogError($"No NAIFCatalogDatabase asset found");
            _isUpdatingHorizonsDatabase = false;
            yield break;
        }

        ResetRuntimeDatabaseChangesState();

        using UnityWebRequest www = UnityWebRequest.Get(_majorBodiesCatalogURL);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Error: {www.error}");
            Debug.LogError($"Response Code: {www.responseCode}");
            ResetRuntimeDatabaseChangesState();
            _isUpdatingHorizonsDatabase = false;
            yield break;
        }
        else
        {
            _rawHorizonCatalogResponse = www.downloadHandler.text;
            HorizonsResponse response = HorizonsResponse.CreateFromJSON(_rawHorizonCatalogResponse);
            List<string> formattedResponse = HorizonsParser.FormatResponse(response);
            if (HorizonsParser.TryParseCatalog(formattedResponse, out List<BodyCatalog> catalogParsed))
            {
                if (!IsSameCatalogDatabase(catalogParsed, _horizonsCatalogDB))
                {
                    if (!JSONCatalog.TryStoreCatalogDBAsJSON(catalogParsed, JSONCatalog.CatalogDatabaseFileName, JSONCatalog.CatalogDatabaseFolderName))
                    {
                        ResetRuntimeDatabaseChangesState();
                        _isUpdatingHorizonsDatabase = false;
                        yield break;
                    }
                    else
                    {
                        _horizonsCatalogDB = new List<BodyCatalog>(catalogParsed);

                        if (!TryMergeCatalogDB(_horizonsCatalogDB, _userCatalogDB, out _runtimeCatalogDB, out List<string> mergeChanges))
                        {
                            ResetRuntimeDatabaseChangesState();
                            Debug.LogWarning("Merge failed after Horizons update.");
                            _isUpdatingHorizonsDatabase = false;
                            yield break;
                        }
                        else if (!QueryManager.TrySetQueryCatalog(_runtimeCatalogDB))
                        {
                            ResetRuntimeDatabaseChangesState();
                            Debug.LogWarning("Failed to set runtime catalog after Horizons update.");
                            _isUpdatingHorizonsDatabase = false;
                            yield break;
                        }

                        _uiDatabaseController.UpdateUICatalogDB();
                        _didRuntimeDatabaseChange = true;
                        _runtimeDatabaseChanges = new List<string>(mergeChanges);
                    }
                }
            }
            else
            {
                ResetRuntimeDatabaseChangesState();
                Debug.LogError($"Could not parse catalog from 'formattedResponse'");
            }
        }

        _isUpdatingHorizonsDatabase = false;
    }

    bool TryMergeCatalogDB(List<BodyCatalog> horizonCatalog, List<BodyCatalog> userCatalog, out List<BodyCatalog> mergedCatalogs, out List<string> mergeChanges)
    {
        mergedCatalogs = (horizonCatalog != null && horizonCatalog.Count > 0) ? new(horizonCatalog) : new();
        mergeChanges = new();

        if (horizonCatalog == null)
        {
            Debug.LogError($"Invalid BodyCatalog lists. Could not merge catalogs.");
            return false;
        }

        userCatalog ??= new();
        HashSet<int> userSeen = new();
        HashSet<int> horizonIds = new(horizonCatalog.Count);
        for (int i = 0; i < horizonCatalog.Count; i++)
            horizonIds.Add(horizonCatalog[i].NAIFID);

        for (int i = 0; i < userCatalog.Count; i++)
        {
            BodyCatalog userEntry = userCatalog[i];
            int NAIFID = userEntry.NAIFID;

            if (NAIFID == -1)
            {
                mergeChanges.Add("Ignored user-defined NAIFID: -1 (reserved invalid ID)");
                continue;
            }

            if (horizonIds.Contains(NAIFID))
            {
                mergeChanges.Add($"Ignored user-defined NAIFID: {NAIFID} (already exists in Horizons)");
                continue;
            }

            if (!userSeen.Add(NAIFID))
            {
                mergeChanges.Add($"Duplicate user NAIFID ignored: {NAIFID}");
                continue;
            }

            mergedCatalogs.Add(userEntry);
            mergeChanges.Add($"Added user-defined NAIFID: {NAIFID}");
        }

        return true;
    }

    bool IsSameCatalogDatabase(List<BodyCatalog> catalog1, List<BodyCatalog> catalog2)
    {
        if (catalog1 == null || catalog2 == null) return false;
        if (catalog1.Count != catalog2.Count) return false;

        Dictionary<int, BodyCatalog> _cata2 = new(catalog2.Count);
        Dictionary<int, BodyCatalog> _cata1 = new(catalog1.Count);
        for (int i = 0; i < catalog2.Count; i++)
            if (!_cata2.TryAdd(catalog2[i].NAIFID, catalog2[i])) return false;
        for (int i = 0; i < catalog1.Count; i++)
            if (!_cata1.TryAdd(catalog1[i].NAIFID, catalog1[i])) return false;

        foreach (var catalog in _cata1)
        {
            if (!_cata2.ContainsKey(catalog.Key)) return false;
            else
            {
                BodyCatalog catalogX = catalog.Value;
                _cata2.TryGetValue(catalog.Key, out BodyCatalog catalogY);

                static string Norm(string s) => string.IsNullOrWhiteSpace(s) ? "" : s.Trim();
                bool isNameSimilar = string.Equals(Norm(catalogX.Name), Norm(catalogY.Name), StringComparison.OrdinalIgnoreCase);
                bool isDesignationSimilar = string.Equals(Norm(catalogX.Designation), Norm(catalogY.Designation), StringComparison.OrdinalIgnoreCase);
                bool isAliasSimilar = string.Equals(Norm(catalogX.Aliases), Norm(catalogY.Aliases), StringComparison.OrdinalIgnoreCase);

                if (!isNameSimilar || !isDesignationSimilar || !isAliasSimilar) return false;
            }
        }

        return true;
    }

    void ResetRuntimeDatabaseChangesState()
    {
        _runtimeDatabaseChanges.Clear();
        _didRuntimeDatabaseChange = false;
    }

    // User runtime catalog additions
    public bool TryAddUserCatalogEntry(BodyCatalog catalogToAdd)
    {
        if (catalogToAdd.NAIFID == -1)
        {
            UIMessage.Instance.NewUIMessage(MessageType.Error, "Could not add the new catalog entry; Invalid NAIFID: -1", "Invalid NAIFID");
            Debug.LogError($"[NAIFCatalogManager] TryAddUserCatalogEntry(): Could not add the new catalog entry; Invalid NAIFID: -1");
            return false;
        }

        _userCatalogDB ??= new List<BodyCatalog>();

        if (_runtimeCatalogDB != null && _runtimeCatalogDB.Exists(x => x.NAIFID == catalogToAdd.NAIFID))
        {
            UIMessage.Instance.NewUIMessage(MessageType.Error, $"Could not add the new catalog entry; NAIFID '{catalogToAdd.NAIFID}' already exists in the database.", "Duplicate NAIFID");
            Debug.LogError($"Cannot add NAIFID '{catalogToAdd.NAIFID}', already exists in runtime catalog database.");
            return false;
        }

        if (_userCatalogDB.Exists(x => x.NAIFID == catalogToAdd.NAIFID)) // extra guard but in practice, _userCatalogDB should always be apart of _runtimeCatalogDB
        {
            Debug.LogError($"User catalog database already contains NAIFID '{catalogToAdd.NAIFID}'.");
            return false;
        }

        catalogToAdd.Name = catalogToAdd.Name?.Trim();
        catalogToAdd.Designation = catalogToAdd.Designation?.Trim();
        catalogToAdd.Aliases = catalogToAdd.Aliases?.Trim();

        if (string.IsNullOrWhiteSpace(catalogToAdd.Name) && string.IsNullOrWhiteSpace(catalogToAdd.Designation) && string.IsNullOrWhiteSpace(catalogToAdd.Aliases))
            Debug.LogWarning($"NAIFID '{catalogToAdd.NAIFID}' has no Name/Designation/Aliases set (allowed).");

        _userCatalogDB.Add(catalogToAdd);

        if (!JSONCatalog.TryStoreCatalogDBAsJSON(_userCatalogDB, JSONCatalog.UserCatalogDatabaseFileName, JSONCatalog.CatalogDatabaseFolderName))
        {
            _userCatalogDB.RemoveAt(_userCatalogDB.Count - 1);
            return false;
        }
        else
        {
            if (!TryMergeCatalogDB(_horizonsCatalogDB, _userCatalogDB, out _runtimeCatalogDB, out _runtimeDatabaseChanges)) return false;
            else if (!QueryManager.TrySetQueryCatalog(_runtimeCatalogDB)) return false;
            else
            {
                _uiDatabaseController.UpdateUICatalogDB();
                _didRuntimeDatabaseChange = true;
            }
        }

        return true;
    }

    bool TryInitializeCatalogDB()
    {
        ResetRuntimeDatabaseChangesState();

        if (!JSONCatalog.TryLoadLocalCatalogDB(out _userCatalogDB, JSONCatalog.UserCatalogDatabaseFileName, JSONCatalog.CatalogDatabaseFolderName))
        {
            _userCatalogDB = new();
        }
        if (!JSONCatalog.TryLoadLocalCatalogDB(out _horizonsCatalogDB, JSONCatalog.CatalogDatabaseFileName, JSONCatalog.CatalogDatabaseFolderName))
        {
            _horizonsCatalogDB = new();
            StartCoroutine(UpdateHorizonsDatabase());
        }
        else if (AutoUpdateDatabase) StartCoroutine(UpdateHorizonsDatabase());

        if (!TryMergeCatalogDB(_horizonsCatalogDB, _userCatalogDB, out _runtimeCatalogDB, out List<string> mergeChanges))
        {
            Debug.LogError("Could not merge Horizon and User catalog databases.");
            return false;
        }
        else
        {
            if (QueryManager.TrySetQueryCatalog(_runtimeCatalogDB))
            {
                _runtimeDatabaseChanges = new(mergeChanges);
                _didRuntimeDatabaseChange = false;
            }
            else
            {
                Debug.LogWarning("Failed to initalize catalog database; Could not set runtime catalog database");
                return false;
            }
        }

        return true;
    }
}
