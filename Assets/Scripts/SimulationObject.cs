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

    void OnEnable() => RenderSpace.OnOriginChanged += HandleOriginChanged;

    void OnDisable() => RenderSpace.OnOriginChanged -= HandleOriginChanged;

    void HandleOriginChanged(double3 oldOrigin, double3 newOrigin) => UpdateTransform();

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

        if (!NBodyManager.Instance.TrySetObjectVelocity((AstronomicalObject)this, velocity)) return;

        Velocity = velocity;
    }

    public bool SetPosition(double3 position, bool isPlayer = false, AstronomicalObject relativeToObject = null)
    {
        if (!math.all(math.isfinite(position)))
        {
            Debug.LogError($"Invalid Position in SetPosition: {position}");
            return false;
        }

        if (NBodyManager.Instance.IsPositionOccupied(this, position)) return false;

        if (!isPlayer)
        {
            if (!NBodyManager.Instance.TrySetObjectPosition((AstronomicalObject)this, position, relativeToObject)) return false;
        }

        if (isPlayer) RenderSpace.SetOrigin(position);

        Position = position;

        UpdateTransform();

        return true;
    }

    public virtual bool SetPositionNear(SimulationObject targetObject)
    {
        if (targetObject == null) return false;

        double3 targetPosition = targetObject.Position;
        double3 direction = Position - targetPosition;

        if (math.lengthsq(direction) <= 1e-12) direction = new double3(1, 0, 0);
        else direction = math.normalize(direction);

        double safeDistance = NBodyManager.Instance.GetSafeDistanceBetweenObjects(this, targetObject);
        safeDistance *= 1.05;

        double3 finalPosition = targetPosition + direction * safeDistance;

        double actualDistance = math.distance(finalPosition, targetPosition);
        Debug.Log($"[Teleport] Target={targetObject.name} | SafeDistance={safeDistance:E6} | ActualDistance={actualDistance:E6} | TargetRadius={targetObject.GetCollisionRadius():E6}");

        if (NBodyManager.Instance.IsPositionOccupied(this, finalPosition))
        {
            Debug.LogWarning("[Teleport] Position still occupied.");
            return false;
        }

        SetPosition(finalPosition, this is MovementController);
        return true;
    }

    public virtual double GetCollisionRadius(bool addPadding = false) => 0.0;

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
