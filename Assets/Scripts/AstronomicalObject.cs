using UnityEngine;
using Unity.Mathematics;

public class AstronomicalObject : MonoBehaviour
{
    public BodyData Data;

    public double3 Velocity;
    public double3 Position;

    public bool Initialized = false;

    [SerializeField] MeshRenderer _meshRenderer;

    void Awake()
    {
        if (_meshRenderer == null) TryGetComponent(out _meshRenderer);
        Initialize();
    }

    public void UpdateVisualPosition()
    {
        if (!math.all(math.isfinite(Position)))
        {
            Debug.LogError($"{name}: Invalid Position in SyncPosition: {Position}");
            return; // prevents writing NaN into Transform 
        }

        transform.position = (Vector3)(float3)Position;
    }

    void Initialize()
    {
        if (Data == null)
        {
            Debug.LogError($"[AstronomicalObject.cs] Awake(): Missing 'BodyData'. Cannot Initialize.");
            return;
        }

        if (!Initialized)
        {
            // Init pos/vel
            Position = Data.StartPosition;
            Velocity = Data.StartVelocity;

            // Init size
            float UnityDiameter = (float)PhysicsConstants.ToUnityUnitsFromM(Data.Diameter);
            if (UnityDiameter > 0) transform.localScale = Vector3.one * UnityDiameter;
            else Debug.LogWarning($"Diameter is too small for {Data.Name}");

            // Init Material/Appearance
            if (Data.VisualAppearance != null && _meshRenderer != null) _meshRenderer.material = Data.VisualAppearance;
            else Debug.LogWarning($"No material assigned for {Data.Name}");

            UpdateVisualPosition();

            Initialized = true;
        }
    }
}

