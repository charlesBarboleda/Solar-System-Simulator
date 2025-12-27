using Unity.Mathematics;
using UnityEngine;

public class SimulationObject : MonoBehaviour, ISimulationObject
{
    public double3 Position { get; set; }
    public double3 Velocity { get; set; }

    [Header("Debugging Vectors")]
    [SerializeField] double3 _authoritativePosition;
    [SerializeField] Vector3 _localPosition;

    public virtual void UpdateTransform()
    {
        if (!math.all(math.isfinite(Position)))
        {
            Debug.LogError($"Invalid Position in UpdateTransform: {Position}");
            return;
        }

        transform.position = (Vector3)(float3)RenderSpace.ToLocal(Position);
    }

    void LateUpdate()
    {
        _authoritativePosition = Position;
        _localPosition = (Vector3)(float3)RenderSpace.ToLocal(Position);
    }
}
