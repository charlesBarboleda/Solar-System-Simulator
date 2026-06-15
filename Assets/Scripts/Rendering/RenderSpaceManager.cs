using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class RenderSpaceManager : MonoBehaviour
{
    public static RenderSpaceManager Instance { get; private set; }

    public IReadOnlyList<SimulationObject> SimulationObjects => _simulationObjects;
    public IReadOnlyList<AstronomicalObject> NearbyObjects => _nearbyObjects;
    public IReadOnlyList<AstronomicalObject> VisibleObjects => _visibleObjects;
    public IReadOnlyList<AstronomicalObject> NearbyVisibleObjects => _nearbyVisibleObjects;

    public AstronomicalObject ClosestVisibleObject { get; private set; }

    [Header("References")]
    [SerializeField] Camera _playerCamera;
    [SerializeField] Transform _playerTransform;

    [Header("Spatial Query Settings")]
    [SerializeField] float _nearbyDistanceUnity = 5000f;
    [SerializeField] float _visibilityRefreshInterval = 0.2f;

    readonly List<SimulationObject> _simulationObjects = new();
    readonly List<AstronomicalObject> _nearbyObjects = new();
    readonly List<AstronomicalObject> _visibleObjects = new();
    readonly List<AstronomicalObject> _nearbyVisibleObjects = new();

    float _visibilityTimer;

    SimulationObject AnchorObject => RenderSpace.Anchor;
    SimulationObject PlayerSimObject => MovementController.Instance;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        InitializeReferences();
        Initialize();
    }

    void LateUpdate()
    {
        if (_simulationObjects.Count == 0) return;

        UpdateRenderOriginIfNeeded();

        for (int i = 0; i < _simulationObjects.Count; i++)
        {
            _simulationObjects[i].UpdateTransform();
        }

        // Refresh proximity / visibility on a timer
        _visibilityTimer += Time.deltaTime;
        if (_visibilityTimer >= _visibilityRefreshInterval)
        {
            _visibilityTimer = 0f;
            RefreshSpatialCaches();
        }
    }

    void InitializeReferences()
    {
        if (_playerTransform == null && MovementController.Instance != null)
            _playerTransform = MovementController.Instance.transform;

        if (_playerCamera == null) _playerCamera = Camera.main;
    }

    public void Initialize()
    {
        _simulationObjects.Clear();

        if (PlayerSimObject != null) _simulationObjects.Add(PlayerSimObject);

        if (NBodyManager.Instance != null && NBodyManager.Instance.SystemBodies != null && NBodyManager.Instance.SystemBodies.Count > 0)
        {
            foreach (AstronomicalObject astroObject in NBodyManager.Instance.SystemBodies)
                if (astroObject != null) _simulationObjects.Add(astroObject);
        }

        RefreshSpatialCaches();

        Debug.Log($"RenderSpaceManager initialized with {_simulationObjects.Count} simulation objects.");
    }

    public void AddSimulationObject(SimulationObject simulationObject)
    {
        if (simulationObject == null) return;
        if (!_simulationObjects.Contains(simulationObject)) _simulationObjects.Add(simulationObject);
    }

    public void RemoveSimulationObject(SimulationObject simulationObject)
    {
        if (simulationObject == null) return;
        _simulationObjects.Remove(simulationObject);
    }

    void UpdateRenderOriginIfNeeded()
    {
        if (AnchorObject == null) return;

        double3 distanceBetween = AnchorObject.Position - RenderSpace.Origin;

        if (math.length(distanceBetween) > RenderSpace.RenderingThresholdDistance) RenderSpace.SetOrigin(AnchorObject.Position);
    }

    void RefreshSpatialCaches()
    {
        _nearbyObjects.Clear();
        _visibleObjects.Clear();
        _nearbyVisibleObjects.Clear();
        ClosestVisibleObject = null;

        if (_playerTransform == null) InitializeReferences();

        if (_playerCamera == null || _playerTransform == null) return;

        double3 playerSimPos = PlayerSimObject != null ? PlayerSimObject.Position : RenderSpace.Origin;

        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(_playerCamera);

        double closestVisibleDistance = double.MaxValue;

        for (int i = 0; i < _simulationObjects.Count; i++)
        {
            SimulationObject simObj = _simulationObjects[i];
            if (simObj == null) continue;

            if (simObj is not AstronomicalObject astroObject) continue;

            double distanceToPlayer = math.distance(playerSimPos, astroObject.Position);

            bool isNearby = distanceToPlayer <= _nearbyDistanceUnity;
            if (isNearby) _nearbyObjects.Add(astroObject);

            bool isVisible = false;
            if (isNearby)
            {
                float radius = Mathf.Max(0.1f, astroObject.GetSafetyRadiusUnity());
                Vector3 worldPos = astroObject.transform.position;

                Bounds bounds = new Bounds(worldPos, Vector3.one * (radius * 2f));
                isVisible = GeometryUtility.TestPlanesAABB(frustumPlanes, bounds);

                if (isVisible)
                    _visibleObjects.Add(astroObject);
            }

            if (isNearby && isVisible)
            {
                _nearbyVisibleObjects.Add(astroObject);

                if (distanceToPlayer < closestVisibleDistance)
                {
                    closestVisibleDistance = distanceToPlayer;
                    ClosestVisibleObject = astroObject;
                }
            }
        }
    }

    public bool TryGetClosestVisiblePlanet(out AstronomicalObject planet)
    {
        planet = null;

        if (_nearbyVisibleObjects.Count == 0)
            return false;

        double bestDistance = double.MaxValue;

        double3 playerPos = PlayerSimObject != null ? PlayerSimObject.Position : RenderSpace.Origin;

        for (int i = 0; i < _nearbyVisibleObjects.Count; i++)
        {
            AstronomicalObject obj = _nearbyVisibleObjects[i];
            if (obj == null) continue;
            if (obj.Data.Body.Type != BodyType.Planet) continue;

            double d = math.distance(playerPos, obj.Position);
            if (d < bestDistance)
            {
                bestDistance = d;
                planet = obj;
            }
        }

        return planet != null;
    }

    public bool TryGetClosestVisibleObject(out AstronomicalObject obj)
    {
        obj = ClosestVisibleObject;
        return obj != null;
    }
}