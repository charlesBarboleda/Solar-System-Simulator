using System;
using Unity.Mathematics;

public static class RenderSpace
{
    public static double3 Origin; // The current floating render origin used to convert between global and local render-space positions.
    public static SimulationObject Anchor; // The object that render space is currently centered or anchored around.
    public static double RenderingThresholdDistance = 1000.0; // The distance at which the render origin should typically be recentered.
    public static event Action<double3, double3> OnOriginChanged; // Fired when the render origin changes, passing old and new origin values.
    public static event Action<SimulationObject, SimulationObject> OnAnchorChanged; // Fired when the anchor changes, passing old and new anchor references.

    public static double3 ToLocal(double3 global) => global - Origin; // Converts a global-space position into local render-space coordinates.
    public static double3 ToGlobal(double3 local) => local + Origin; // Converts a local render-space position back into global-space coordinates.

    // Updates the current render origin
    public static void SetOrigin(double3 newOrigin)
    {
        double3 oldOrigin = Origin;
        Origin = newOrigin;

        OnOriginChanged?.Invoke(oldOrigin, newOrigin);
    }

    // Updates the current anchor object
    public static void SetAnchor(SimulationObject newAnchor)
    {
        SimulationObject oldAnchor = Anchor;
        Anchor = newAnchor;

        OnAnchorChanged?.Invoke(oldAnchor, newAnchor);
    }
}