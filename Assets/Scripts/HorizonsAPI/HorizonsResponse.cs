using System;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public class HorizonsResponse
{
    public string result;
    public HorizonSignature signature;

    public static HorizonsResponse CreateFromJSON(string jsonResult)
    {
        return JsonUtility.FromJson<HorizonsResponse>(jsonResult);
    }
}

[Serializable]
public class HorizonSignature
{
    public string version;
    public string source;
}
