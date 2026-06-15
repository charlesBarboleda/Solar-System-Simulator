using System;
using System.Collections.Generic;
using System.Numerics;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public class SaveSlotMeta
{
    public string Id;           // GUID, used as the filename key
    public string DisplayName;  // User-facing name
    public string SavedAt;
    public int BodyCount;
    public double SimDays;

    public DateTime SavedAtDateTime => DateTime.TryParse(SavedAt, out var dt) ? dt : DateTime.MinValue;
}

[Serializable]
public class SimulationSaveIndex
{
    public List<SaveSlotMeta> Slots = new();
}

[Serializable]
public class SimulationSaveData
{
    public SaveSlotMeta Meta;

    // Simulation settings
    public bool IsPaused;
    public double CurrentSimDays;
    public GravityModel GravityModel;
    public AccelBMode AccelBMode;
    public int AccelIterations;
    public double TimeScale;
    public double GravityScale;
    public double FixedStepSimDays;
    public int MaxSubstepsPerFixedUpdate;
    public double MaxBacklogSimDays;

    // Body states
    public List<BodyStateData> Bodies = new();
}

[Serializable]
public class BodyStateData
{
    public string Name;
    public Double3DTO Position;
    public Double3DTO Velocity;
    public UnityEngine.Quaternion Rotation;
}

[Serializable]
public struct Double3DTO
{
    public double x, y, z;

    public Double3DTO(double3 value) { x = value.x; y = value.y; z = value.z; }
    public double3 ToDouble3() => new(x, y, z);
}