using System;
using Unity.Mathematics;

public static class RenderSpace
{
    public static double3 Origin;
    public static SimulationObject Anchor;
    public static double RenderingThresholdDistance = 1000.0;
    public static event Action<double3, double3> OnOriginChanged;
    public static double3 ToLocal(double3 global) => global - Origin;
    public static double3 ToGlobal(double3 local) => local + Origin;
    public static void SetOrigin(double3 newOrigin) => Origin = newOrigin;
    public static void SetAnchor(SimulationObject anchor) => Anchor = anchor;

}
