using System;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public class HorizonsResponse
{
    public string result;
    public string signature;

    public static HorizonsResponse CreateFromJSON(string jsonResult)
    {
        return JsonUtility.FromJson<HorizonsResponse>(jsonResult);
    }
}
