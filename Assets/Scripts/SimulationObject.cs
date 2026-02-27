using Unity.Mathematics;
using UnityEngine;

public class SimulationObject : MonoBehaviour, ISimulationObject
{
    public double3 Position { get; set; }
    public double3 Velocity { get; set; }

    [Header("Debugging Vectors")]
    [SerializeField] bool _debugPositions = false;
    [SerializeField] double3 _authoritativePosition;
    [SerializeField] double3 _authoritativeVelocity;
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

    public void SetVelocity(double3 velocity)
    {
        if (!math.all(math.isfinite(velocity)))
        {
            Debug.LogError($"Invalid Velocity in SetVelocity: {velocity}");
            return;
        }

        Velocity = velocity;
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

    public Vector3 GetLocalPosition() => (Vector3)(float3)RenderSpace.ToLocal(Position);

    public double3 GetGlobalPosition() => RenderSpace.ToGlobal(Position);


    void LateUpdate()
    {
        if (_debugPositions)
        {
            _authoritativeVelocity = Velocity;
            _authoritativePosition = Position;
            _localPosition = (Vector3)(float3)RenderSpace.ToLocal(Position);
        }
    }
}
