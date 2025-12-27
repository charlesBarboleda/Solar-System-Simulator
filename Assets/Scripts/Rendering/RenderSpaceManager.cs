using Unity.Mathematics;
using UnityEngine;

public class RenderSpaceManager : MonoBehaviour
{
    SimulationObject AnchorObject => RenderSpace.Anchor;
    SimulationObject[] _simulationObjects;

    void Start()
    {
        // Find all SimulationObjects in the scene
        _simulationObjects = FindObjectsByType<SimulationObject>(FindObjectsSortMode.None);
    }

    void LateUpdate()
    {
        if (AnchorObject == null || _simulationObjects == null || _simulationObjects.Length == 0)
            return;

        double3 delta = AnchorObject.Position - RenderSpace.Origin;

        if (math.length(delta) > RenderSpace.RenderingThresholdDistance)
        {
            RenderSpace.SetOrigin(AnchorObject.Position);

        }

        for (int i = 0; i < _simulationObjects.Length; i++)
        {
            _simulationObjects[i].UpdateTransform();
        }
    }
}
