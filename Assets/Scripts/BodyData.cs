using UnityEngine;
using Unity.Mathematics;

[System.Serializable]
public struct BodyData
{
    public string Name;

    public BodyType Type;

    [Tooltip("Mass in KILOGRAMS")]
    public double Mass;

    [Tooltip("Diameter in METERS")]
    public double Diameter;

    public double3 StartVelocity;
    public double3 StartPosition;

    public Material VisualAppearance;
}