using Unity.Mathematics;
using UnityEngine;

public class RenderSpaceManager : MonoBehaviour
{
    SimulationObject AnchorObject => RenderSpace.Anchor;
    public SimulationObject[] SimulationObjects;

    void Start()
    {
        // Find all SimulationObjects in the scene
        SimulationObjects = FindObjectsByType<SimulationObject>(FindObjectsSortMode.None);
    }

    void LateUpdate()
    {
        if (AnchorObject == null || SimulationObjects == null || SimulationObjects.Length == 0)
            return;

        double3 delta = AnchorObject.Position - RenderSpace.Origin;

        if (math.length(delta) > RenderSpace.RenderingThresholdDistance)
        {
            RenderSpace.SetOrigin(AnchorObject.Position);

        }

        for (int i = 0; i < SimulationObjects.Length; i++)
        {
            SimulationObjects[i].UpdateTransform();
        }
    }
}
