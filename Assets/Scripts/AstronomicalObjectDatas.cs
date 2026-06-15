using UnityEngine;
using Unity.Mathematics;
using System;

[Serializable]
public struct RotationData
{
    [Tooltip("Rotation Rate in RAD/S. Determines how fast the object spins per simulation second. You can only use one of Rotation Rate OR Mean Sidereal Day. Set to 0 if using Mean Sidereal Day.")]
    public double RotationRate;

    [Tooltip("Mean Sidereal Day in HOURS. Determines how many simulation hours it takes for a full rotation. You can only use one of Rotation Rate OR Mean Sidereal Day. Set to 0 if using Rotation Rate.")]
    public double MeanSiderealDay;

    [Tooltip("Axial tilt in DEGREES. This is how far the body's spin axis leans away from the simulation's up direction. Example: Earth is about 23.44 degrees.")]
    public double AxialTiltDeg;

    [Tooltip("Tilt direction in DEGREES around the simulation's up axis. This determines which horizontal direction the north pole leans toward. Example convention: 0 = +Z, 90 = +X, 180 = -Z, 270 = -X.")]
    public double AxisAzimuthDeg;

    [Tooltip("Initial spin angle in DEGREES around the body's own spin axis at simulation start. Use this to decide which face of the planet is visible first.")]
    public double InitialSpinDeg;

    [Tooltip("Manual mesh/texture correction in DEGREES. Use this when the model's visual prime meridian does not line up with the simulation's expected 0-degree longitude direction.")]
    public double ModelPrimeMeridianOffset;

    [Tooltip("If enabled, the body spins in the opposite direction. Useful for retrograde rotators such as Venus.")]
    public bool Retrograde;

    [Tooltip("Used only for 'Basic' rotation")]
    public double RotationPeriod;

    [Tooltip("Specify Rotation Mode")]
    public bool IsBasicRotation;

}

[Serializable]
public struct PositionData
{
    [Tooltip("Starting position in simulation/world units.")]
    public double3 StartPosition;
}


[Serializable]
public struct VelocityData
{
    [Tooltip("Starting velocity in simulation/world units.")]
    public double3 StartVelocity;
}

[Serializable]
public struct BodyData
{
    public string Name;

    public BodyType Type;

    [Tooltip("Mass in KILOGRAMS")]
    public double Mass;

    [Tooltip("Diameter in METERS")]
    public double Diameter;

    [Tooltip("Temperature in KELVIN")]
    public double Temperature;
}

[Serializable]
public struct RingData
{
    public bool IsRingPlanet;
    [Tooltip("Inner gap of the ring in KILOMETERS. This is the distance from the center of the planet to the inner edge of the ring.")]
    public double InnerGapKM;

    [Tooltip("Width of the ring in KILOMETERS. This is how wide the ring is from its inner edge to its outer edge.")]
    public double RingWidthKM;
}

[Serializable]
public struct VisualData
{
    public string MaterialName;
}

[Serializable]
public struct DisplayData
{
    public Texture2D DisplayImage;

    public string DisplayImageFileName;
}

[Serializable]
public struct Data
{
    public BodyData Body;
    public PositionData Position;
    public VelocityData Velocity;
    public RotationData Rotation;
    public VisualData Visual;
    public RingData Ring;

    public DisplayData Display;
}