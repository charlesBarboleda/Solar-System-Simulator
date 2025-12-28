using Unity.Mathematics;
using UnityEngine;

public class SimulationObject : MonoBehaviour, ISimulationObject
{
    public double3 Position { get; set; }
    public double3 Velocity { get; set; }

    [Header("Debugging Vectors")]
    [SerializeField] bool _debugPositions = false;
    [SerializeField] double3 _authoritativePosition;
    [SerializeField] Vector3 _localPosition;

    public void UpdateTransform()
    {
        if (!math.all(math.isfinite(Position)))
        {
            Debug.LogError($"Invalid Position in UpdateTransform: {Position}");
            return;
        }

        transform.position = (Vector3)(float3)RenderSpace.ToLocal(Position);
    }

    public void TeleportTo(double3 newPosition)
    {
        Position = newPosition;
        UpdateTransform();
    }

    public void TeleportTo(SimulationObject targetObject)
    {
        Position = targetObject.Position;
        UpdateTransform();
    }

    void LateUpdate()
    {
        if (_debugPositions)
        {
            _authoritativePosition = Position;
            _localPosition = (Vector3)(float3)RenderSpace.ToLocal(Position);
        }
    }
}
