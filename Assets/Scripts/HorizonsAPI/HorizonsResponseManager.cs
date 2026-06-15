using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using NaughtyAttributes;
using System.Collections.Generic;

public class HorizonsResponseManager : MonoBehaviour
{
    public static HorizonsResponseManager Instance { get; private set; }
    // Result
    public string RawResponse { get; private set; }
    public HorizonsResponse Response { get; private set; }
    public List<string> FormattedResponse { get; private set; }
    public string ErrorResponse { get; private set; }
    public bool IsSuccessful { get; private set; }
    public string ResponseCode { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        IsSuccessful = false;
    }

    public IEnumerator GetHorizonsResponse(string URL)
    {
        using UnityWebRequest www = UnityWebRequest.Get(URL);
        yield return www.SendWebRequest();

        ResetResponse();
        if (www.result != UnityWebRequest.Result.Success)
        {
            IsSuccessful = false;
            ErrorResponse = www.error;
            ResponseCode = www.responseCode.ToString();
            yield break;
        }
        else
        {
            IsSuccessful = true;
            RawResponse = www.downloadHandler.text;
            Debug.Log($"Raw Response: {RawResponse}");
            HorizonsResponse response = HorizonsResponse.CreateFromJSON(RawResponse);
            Response = response;
            FormattedResponse = HorizonsParser.FormatResponse(response);
        }
    }

    void ResetResponse()
    {
        RawResponse = string.Empty;
        ErrorResponse = string.Empty;
        ResponseCode = string.Empty;
        FormattedResponse = null;
        Response = null;
        IsSuccessful = false;
    }

    [Button]
    void HorizonTest()
    {
        string positionURL = "https://ssd.jpl.nasa.gov/api/horizons.api?format=json&COMMAND=399&OBJ_DATA='NO'&MAKE_EPHEM='YES'&EPHEM_TYPE='VECTORS'&REF_PLANE='ECLIPTIC'&START_TIME=%271ad-Jan-01%2001%3A01%3A01.000%27&STOP_TIME=%272ad-Feb-02%2002%3A02%3A02.000%27&STEP_SIZE=%271%20mo%27";
        Debug.Log($"API Request URL: {positionURL}");
        StartCoroutine(GetHorizonsResponse(positionURL));
    }
}

