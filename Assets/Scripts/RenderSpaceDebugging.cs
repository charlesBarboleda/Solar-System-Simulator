using UnityEngine;
using Unity.Mathematics;

public class RenderSpaceDebugger : MonoBehaviour
{
    public SimulationObject Anchor;
    public bool DrawGizmos = true;
    public bool ShowHUD = true;

    void OnDrawGizmos()
    {
        if (!DrawGizmos) return;

        // Threshold boundary in LOCAL (Unity float) space
        Gizmos.DrawWireSphere(Vector3.zero, (float)RenderSpace.RenderingThresholdDistance);

        if (Anchor != null)
        {
            // Line from local origin to the anchor's *rendered* position
            Gizmos.DrawLine(Vector3.zero, Anchor.transform.position);
        }
    }

    void OnGUI()
    {
        if (!ShowHUD || Anchor == null) return;

        double3 origin = RenderSpace.Origin;
        double3 global = Anchor.Position;
        double3 localD = RenderSpace.ToLocal(global);

        GUILayout.Label($"RenderSpace.Origin (global double): {origin}");
        GUILayout.Label($"Anchor.Position (global double):      {global}");
        GUILayout.Label($"Anchor local (global-origin):         {localD}");
        GUILayout.Label($"Anchor transform.position (float):    {Anchor.transform.position}");
        GUILayout.Label($"Threshold (local units):              {RenderSpace.RenderingThresholdDistance}");
    }
}
