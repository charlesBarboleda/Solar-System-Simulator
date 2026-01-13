using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Globalization;

public class HorizonsAPIManager : MonoBehaviour
{
    [Header("Search Settings")]
    public HorizonFormat FormatType;
    public string BodyID;
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
    [SerializeField] bool _lowerCase = false;

    IEnumerator GetResult(string URL)
    {
        UnityWebRequest www = UnityWebRequest.Get(URL);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(www.error);
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
            var bodyDatas = HorizonsParser.ParseBodyData(formattedResponse);
            string empty = "";
            foreach (var data in bodyDatas)
            {
                Debug.Log($"{data.Key}: {data.Value.NumericValue} {(data.Value.NumericValueUnit == UnitMeasurements.None ? empty : HorizonsParser.UnitMeasurementsToString(data.Value.NumericValueUnit))}");
            }
        }
    }

    [ContextMenu("Run Horizon API search test")]
    void HorizonTest()
    {
        HorizonsSearchSettings _settings = new(
           command: BodyID,
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
        StartCoroutine(GetResult(url));

    }
}

