using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

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
    public bool AutoUpdateDatabase;
    public bool IsChanged;
    public List<string> DatabaseChanges;
    readonly string _majorBodiesCatalogURL = "https://ssd.jpl.nasa.gov/api/horizons.api?format=json&COMMAND='MB'";

    void Awake()
    {
        if (AutoUpdateDatabase) StartCoroutine(AutoUpdateHorizonsDatabase(_majorBodiesCatalogURL));
    }

    IEnumerator GetHorizonsResponse(string URL)
    {
        UnityWebRequest www = UnityWebRequest.Get(URL);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Error: {www.error}");
            Debug.LogError($"Response Code: {www.responseCode}");
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

            // var bodyDatas = HorizonsParser.ParseBodyData(formattedResponse);
            // foreach (var data in bodyDatas)
            // {
            //     Debug.Log($"{data.Key}: {data.Value.NumericValue} {(data.Value.NumericValueUnit == UnitMeasurements.None ? string.Empty : HorizonsParser.UnitMeasurementsToString(data.Value.NumericValueUnit).ToLowerInvariant())}");
            // }
        }
    }

    IEnumerator AutoUpdateHorizonsDatabase(string URL)
    {
        UnityWebRequest www = UnityWebRequest.Get(URL);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Error: {www.error}");
            Debug.LogError($"Response Code: {www.responseCode}");
        }
        else
        {
            HorizonsResults = www.downloadHandler.text;
            HorizonsResponse response = HorizonsResponse.CreateFromJSON(HorizonsResults);
            List<string> formattedResponse = HorizonsParser.FormatResponse(response);
            if (HorizonsParser.TryParseCatalog(formattedResponse, out List<BodyCatalog> catalogParsed))
            {
                if (_database.TryUpdateCatalog(catalogParsed, out var updatedCatalog, out var changes))
                {
                    IsChanged = true;
                    DatabaseChanges = changes;
                    _database.ReplaceCatalog(updatedCatalog);
                }
            }
            else Debug.LogError($"Could not parse catalog from 'formattedResponse'");
        }
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

