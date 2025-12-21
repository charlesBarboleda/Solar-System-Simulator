using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Rendering;
using System;
using System.Drawing;
using Mono.Cecil.Cil;
using Unity.VisualScripting;
using UnityEngine.SocialPlatforms;

public class AstronomicalObject : MonoBehaviour
{
    public BodyData Data;

    public double3 Velocity;
    public double3 Position;

    public bool Initialized = false;
    [SerializeField] MeshRenderer _meshRenderer;

    [Header("Temp Saturn Vars")]
    [SerializeField] Mesh _ringMesh;
    [SerializeField] GameObject _ring;


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

        // Saturn temp init
        if (_ringMesh != null || _ring != null)
        {
            double targetMin = PhysicsConstants.ToUnityUnitsFromM(PhysicsConstants.REAL_SATURN_MIN_RING_DISTANCE_FROM_CENTER_M);
            double targetMax = PhysicsConstants.ToUnityUnitsFromM(PhysicsConstants.REAL_SATURN_MAX_RING_DISTANCE_FROM_CENTER_M);

            double localMinRadius = GetLocalMinRadius(_ringMesh);
            double localMaxRadius = GetLocalMaxRadius(_ringMesh);

            double worldMaxRadius = localMaxRadius * _ring.transform.lossyScale.x;
            double worldMinRadius = localMinRadius * _ring.transform.lossyScale.x;
            Debug.Log($"Old World-Space Min Radius: {worldMinRadius}");
            Debug.Log($"Old World-Space Max Radius: {worldMaxRadius}");

            if (localMinRadius <= 0.0) return;
            if (localMaxRadius <= 0.0) return;

            // Set uniform scale so inner radius matches target
            float newScale = (float)(targetMin / localMinRadius);
            _ring.transform.localScale = Vector3.one * newScale;

            double newLocalMinRadius = GetLocalMinRadius(_ringMesh);
            double newWorldMinRadius = newLocalMinRadius * _ring.transform.lossyScale.x;
            double newLocalMaxRadius = GetLocalMaxRadius(_ringMesh);
            double newWorldMaxRadius = newLocalMaxRadius * _ring.transform.lossyScale.x;

            Debug.Log($"Target Min: {targetMin}");
            Debug.Log($"New World-Space Min Radius: {newWorldMinRadius}");

            Debug.Log($"Target Max: {targetMax}");
            Debug.Log($"New World-Space Max Radius: {newWorldMaxRadius}");

        }
    }

    static double GetLocalMinRadius(Mesh mesh)
    {
        var verts = mesh.vertices;
        double minR = double.PositiveInfinity;

        for (int i = 0; i < verts.Length; i++)
        {
            double x = verts[i].x;
            double z = verts[i].z;
            double r = Math.Sqrt(x * x + z * z);
            if (r < minR) minR = r;
        }

        return minR;
    }

    static double GetLocalMaxRadius(Mesh mesh)
    {
        var verts = mesh.vertices;
        double maxR = 0.0;

        for (int i = 0; i < verts.Length; i++)
        {
            double x = verts[i].x;
            double z = verts[i].z;
            double r = Math.Sqrt(x * x + z * z);
            if (r > maxR) maxR = r;
        }
        return maxR;
    }
}

