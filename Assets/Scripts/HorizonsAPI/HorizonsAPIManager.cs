using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;

public class HorizonsAPIManager : MonoBehaviour
{
    [Header("Search Settings")]
    public HorizonFormat FormatType;
    public BodySearchType BodySearchType;
    public string TestCommandID;
    public string BodyName;
    public int BodyID;
    public BodySearchType CenterSearchType;
    public string CenterName;
    public string CenterID;
    public bool ObjectData;
    public bool MakeEphemeris;
    public EphemerisType EphemerisType;
    public string StartTime;
    public string StopTime;
    public StepSizeUnit StepSizeUnit;
    public int StepSizeValue;
    public ReferencePlane ReferencePlane;
    public ReferenceSystem ReferenceSystem;
    public OutputUnits OutputUnits;
    public int VectorTable;

    // Result
    public string HorizonsResults;

    [Header("Data Parsing")]
    public ParsableData[] ParsableData;

    [Header("Cached NAIF Database")]
    [SerializeField] NAIFCatalogDatabase _database;
    [SerializeField] List<BodyCatalog> _userCatalogDB = new();
    [SerializeField] List<BodyCatalog> _horizonCatalogDB = new();
    [SerializeField] List<BodyCatalog> _runtimeCatalogDB = new();
    public bool AutoUpdateDatabase;
    bool _didRuntimeDatabaseChange;
    public bool DidDatabaseUpdate => _didRuntimeDatabaseChange;
    List<string> _runtimeDatabaseChanges;
    public IReadOnlyList<string> RuntimeDatabaseChanges => _runtimeDatabaseChanges;

    readonly string _majorBodiesCatalogURL = "https://ssd.jpl.nasa.gov/api/horizons.api?format=json&COMMAND='MB'";

    void Awake()
    {
        if (_database == null)
        {
            Debug.LogError("[HorizonsAPIManager] NAIFCatalogDatabase asset reference is missing in the Inspector.");
            enabled = false;
            return;
        }

        ResetRuntimeDatabaseChangesState();

        InitializeRuntimeDatabase();
    }

    void InitializeRuntimeDatabase()
    {
        if (!JSONCatalog.TryLoadLocalCatalogDatabase(out _horizonCatalogDB, JSONCatalog.CatalogDatabaseFileName, JSONCatalog.CatalogDatabaseFolderName))
        {
            _horizonCatalogDB = new();
            StartCoroutine(UpdateHorizonsDatabase());
        }
        else if (AutoUpdateDatabase) StartCoroutine(UpdateHorizonsDatabase());

        if (!JSONCatalog.TryLoadLocalCatalogDatabase(out _userCatalogDB, JSONCatalog.UserCatalogDatabaseFileName, JSONCatalog.CatalogDatabaseFolderName))
            _userCatalogDB = new();

        if (!TryMergeCatalogDB(out _runtimeCatalogDB, out List<string> mergeChanges))
        {
            Debug.LogWarning("Could not merge Horizon and User catalog databases.");
        }
        else
        {
            if (_database.TrySetRuntimeCatalog(_runtimeCatalogDB))
            {
                // Store merge notes for UI/debugging (does NOT count as a Horizons update)
                _runtimeDatabaseChanges = new(mergeChanges);
                _didRuntimeDatabaseChange = false;
            }
            else
            {
                Debug.LogWarning("Failed to set runtime catalog database.");
            }
        }
    }

    IEnumerator GetHorizonsResponse(string URL)
    {
        using UnityWebRequest www = UnityWebRequest.Get(URL);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Error: {www.error}");
            Debug.LogError($"Response Code: {www.responseCode}");
            yield break;
        }
        else
        {
            HorizonsResults = www.downloadHandler.text;
            HorizonsResponse response = HorizonsResponse.CreateFromJSON(HorizonsResults);
            List<string> formattedResponse = HorizonsParser.FormatResponse(response);
            foreach (var line in formattedResponse)
            {
                Debug.Log(line);
            }
        }
    }

    IEnumerator UpdateHorizonsDatabase()
    {
        if (_database == null)
        {
            Debug.LogError($"No NAIFCatalogDatabase asset found");
            yield break;
        }

        using UnityWebRequest www = UnityWebRequest.Get(_majorBodiesCatalogURL);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Error: {www.error}");
            Debug.LogError($"Response Code: {www.responseCode}");
            ResetRuntimeDatabaseChangesState();
            yield break;
        }
        else
        {
            HorizonsResults = www.downloadHandler.text;
            HorizonsResponse response = HorizonsResponse.CreateFromJSON(HorizonsResults);
            List<string> formattedResponse = HorizonsParser.FormatResponse(response);
            if (HorizonsParser.TryParseCatalog(formattedResponse, out List<BodyCatalog> catalogParsed))
            {
                if (!IsSameCatalogDatabase(catalogParsed, _horizonCatalogDB))
                {
                    if (!JSONCatalog.TryStoreCatalogDBAsJSON(catalogParsed, JSONCatalog.CatalogDatabaseFileName, JSONCatalog.CatalogDatabaseFolderName))
                    {
                        ResetRuntimeDatabaseChangesState();
                        yield break;
                    }
                    else
                    {
                        _horizonCatalogDB = new List<BodyCatalog>(catalogParsed);

                        if (!TryMergeCatalogDB(out _runtimeCatalogDB, out List<string> mergeChanges))
                        {
                            ResetRuntimeDatabaseChangesState();
                            Debug.LogWarning("Merge failed after Horizons update.");
                            yield break;
                        }

                        if (!_database.TrySetRuntimeCatalog(_runtimeCatalogDB))
                        {
                            ResetRuntimeDatabaseChangesState();
                            Debug.LogWarning("Failed to set runtime catalog after Horizons update.");
                            yield break;
                        }

                        _didRuntimeDatabaseChange = true;
                        _runtimeDatabaseChanges = new List<string>(mergeChanges);
                    }
                }
                else
                {
                    ResetRuntimeDatabaseChangesState();
                }
            }
            else
            {
                ResetRuntimeDatabaseChangesState();
                Debug.LogError($"Could not parse catalog from 'formattedResponse'");
            }
        }
    }

    bool TryMergeCatalogDB(out List<BodyCatalog> mergedCatalogs, out List<string> mergeChanges)
    {
        mergedCatalogs = (_horizonCatalogDB != null && _horizonCatalogDB.Count > 0) ? new(_horizonCatalogDB) : new();
        mergeChanges = new();

        if (_horizonCatalogDB == null)
        {
            Debug.LogError($"Invalid BodyCatalog lists. Could not merge catalogs.");
            return false;
        }
        _userCatalogDB ??= new();
        HashSet<int> userSeen = new();
        HashSet<int> horizonIds = new(_horizonCatalogDB.Count);
        for (int i = 0; i < _horizonCatalogDB.Count; i++)
            horizonIds.Add(_horizonCatalogDB[i].NAIFID);

        for (int i = 0; i < _userCatalogDB.Count; i++)
        {
            BodyCatalog userEntry = _userCatalogDB[i];
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

                bool isNameSimilar = string.Equals(catalogX.Name?.Trim(), catalogY.Name?.Trim(), StringComparison.OrdinalIgnoreCase);
                bool isDesignationSimilar = string.Equals(catalogX.Designation?.Trim(), catalogY.Designation?.Trim(), StringComparison.OrdinalIgnoreCase);
                bool isAliasSimilar = string.Equals(catalogX.Aliases?.Trim(), catalogY.Aliases?.Trim(), StringComparison.OrdinalIgnoreCase);

                if (!isNameSimilar || !isDesignationSimilar || !isAliasSimilar) return false;
            }
        }

        return true;
    }

    void ResetRuntimeDatabaseChangesState()
    {
        _runtimeDatabaseChanges ??= new List<string>();
        _runtimeDatabaseChanges.Clear();
        _didRuntimeDatabaseChange = false;
    }

    [ContextMenu("Run Horizon API search test")]
    void HorizonTest()
    {
        HorizonsSearchSettings _settings = new(
           commandID: BodyID,
           testCommandID: TestCommandID,
           bodyName: BodyName,
           bodyID: BodyID,
           bodySearchType: BodySearchType,
           coordinateCenter: CenterID,
           startTime: StartTime,
           stopTime: StopTime,
           stepSizeValue: StepSizeValue,
           stepSizeUnit: StepSizeUnit,
           horizonFormatType: HorizonFormat.json,
           objData: ObjectData,
           makeEphemeris: MakeEphemeris,
           ephemerisType: EphemerisType,
           refPlane: ReferencePlane,
           referenceSystem: ReferenceSystem,
           outputUnits: OutputUnits,
           vecTable: VectorTable
       );

        string url = _settings.BuildQuery(_settings);
        Debug.Log($"API Request URL: {url}");
        StartCoroutine(GetHorizonsResponse(url));
    }
}

