using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework.Interfaces;
using UnityEngine;
using UnityEngine.Networking;


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


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

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
            Dictionary<ParsableData, DataValue> formattedResponse = HorizonsParser.FormatResponse(response: response, lowercase: _lowerCase, formattedVectors: out List<EphemerisSample> formattedVectors, rawSplitString: out string[] rawText);
            foreach (var line in rawText)
            {
                Debug.Log(line);
            }
            foreach (var line in formattedResponse)
            {
                Debug.Log($"{line.Key}: {(line.Value.NumericValue != 0.0 ? line.Value.NumericValue : line.Value.RawTextValue)}");
            }
            foreach (var ephemeris in formattedVectors)
            {
                Debug.Log($"Name: {ephemeris.BodyName}\nCenter Body Name: {ephemeris.CenterBodyName}\nDate: {ephemeris.Date}\nPos:{ephemeris.Position}\nVel:{ephemeris.Velocity}");
            }
            // HorizonsParser.TryParseData(ParsableData, formattedResponse, out string[] dataValue);
            // foreach (var value in dataValue)
            // {
            //     Debug.Log(value);
            // }

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

