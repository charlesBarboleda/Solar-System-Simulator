using UnityEngine;
using Unity.Mathematics;

[CreateAssetMenu(menuName = "Create Astronomical Body/Data")]
public class BodyData : ScriptableObject
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
