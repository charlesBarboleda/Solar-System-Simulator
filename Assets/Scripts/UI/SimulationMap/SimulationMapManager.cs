using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using System;

public class SimulationMapManager : MonoBehaviour
{
    public static SimulationMapManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] GameObject _mapObjectPrefabContainer;
    [SerializeField] Transform _mapParent;
    [SerializeField] TextMeshProUGUI _auPerGridLabel;
    [SerializeField] SimulationMapGrid _mapGrid;
    [SerializeField] TextMeshProUGUI _horizontalGridScaleLabel;
    [SerializeField] TextMeshProUGUI _verticalGridScaleLabel;

    [Header("Settings")]
    [SerializeField] float _defaultAUPerGrid = 50f;

    [Header("Optional")]
    [SerializeField] bool _hideObjectsOutsideBounds = false;
    [SerializeField] bool _rotateWithPlayer = true;

    readonly float[] _auGridSteps =
    {
        0.001f, 0.002f, 0.005f,
        0.01f,  0.02f,  0.05f,
        0.1f,   0.2f,   0.5f,
        1f,     2f,     5f,
        10f,    20f,    50f,
        100f,   200f,   500f,
        1000f,  2000f,  5000f,
        10000f, 50000f, 100000f
    };

    int _auGridStepIndex;

    double3 _playerGlobalPosition => MovementController.Instance != null ? MovementController.Instance.GetGlobalPosition() : double3.zero;

    readonly Dictionary<string, MapObjectManager> _simulationMapObjects = new();
    readonly List<MapObjectManager> _mapObjectList = new();

    // Internal value used for object positioning, derived from AUPerGrid
    public float MapScale { get; private set; }
    public float AUPerGrid { get; private set; }

    MapPlane _currentMapPlane = MapPlane.Y;

    enum MapPlane { X, Y, Z }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        SetAUPerGrid(_defaultAUPerGrid);
        _currentMapPlane = MapPlane.Y;
    }

    void Update()
    {
        if (_mapParent == null || !_mapParent.gameObject.activeInHierarchy) return;

        UpdateMapObjects();
        HandleScrollZoom();
        UpdateMapRotation();
    }

    void HandleScrollZoom()
    {
        if (Mouse.current == null) return;

        Vector2 scrollDelta = Mouse.current.scroll.ReadValue();
        if (scrollDelta.y == 0) return;

        if (scrollDelta.y > 0) ZoomIn();
        else ZoomOut();
    }

    void UpdateMapObjects()
    {
        double3 playerPosition = _playerGlobalPosition;
        Vector2 halfBounds = GetMapHalfBounds();

        foreach (MapObjectManager mapObjectManager in _mapObjectList)
        {
            if (mapObjectManager == null) continue;

            double3 relativePosition = mapObjectManager.GetGlobalPosition() - playerPosition;
            Vector2 projectedPosition = ProjectWorldToMap(relativePosition);
            Vector2 scaledPosition = projectedPosition * MapScale;

            if (_hideObjectsOutsideBounds)
            {
                Vector2 screenAlignedPos = (Vector2)(_mapParent.rotation * (Vector3)scaledPosition);
                bool shouldHide = Mathf.Abs(screenAlignedPos.x) > halfBounds.x ||
                                  Mathf.Abs(screenAlignedPos.y) > halfBounds.y;
                mapObjectManager.SetVisible(!shouldHide);
                if (shouldHide) continue;
            }

            mapObjectManager.SetMapPosition(scaledPosition);
            mapObjectManager.SetCounterRotation();
            mapObjectManager.UpdateTrail(playerPosition, MapScale);
        }
    }

    Vector2 GetMapHalfBounds()
    {
        RectTransform mapRect = _mapParent as RectTransform;
        if (mapRect == null) return Vector2.one * float.MaxValue;
        return new Vector2(mapRect.rect.width * 0.5f, mapRect.rect.height * 0.5f);
    }

    public void ClearAllTrails()
    {
        foreach (MapObjectManager mapObj in _mapObjectList)
        {
            if (mapObj == null) continue;
            mapObj.ClearTrail();
        }
    }

    void UpdateMapRotation()
    {
        if (_mapParent == null) return;

        if (!_rotateWithPlayer || MovementController.Instance == null)
        {
            _mapParent.rotation = Quaternion.identity;
            return;
        }

        Vector3 playerForward = MovementController.Instance.GetForwardDirection();
        double3 forwardD3 = new(playerForward.x, playerForward.y, playerForward.z);

        Vector2 projectedForward = ProjectWorldToMap(forwardD3);

        if (projectedForward.sqrMagnitude < 0.001f) return;

        float angle = Vector2.SignedAngle(Vector2.up, projectedForward);
        _mapParent.rotation = Quaternion.Euler(0f, 0f, -angle);
    }

    Vector2 ProjectWorldToMap(double3 relativePosition)
    {
        switch (_currentMapPlane)
        {
            case MapPlane.X: return new Vector2((float)relativePosition.y, (float)relativePosition.z);
            case MapPlane.Y: return new Vector2((float)relativePosition.x, (float)relativePosition.z);
            case MapPlane.Z: return new Vector2((float)relativePosition.x, (float)relativePosition.y);
        }
        return Vector2.zero;
    }

    // AU Per Grid
    public void SetAUPerGrid(float au)
    {
        _auGridStepIndex = FindClosestStepIndex(au);
        ApplyStepIndex();
    }

    void ZoomIn()
    {
        if (_auGridStepIndex > 0)
        {
            _auGridStepIndex--;
            ApplyStepIndex();
        }
    }

    void ZoomOut()
    {
        if (_auGridStepIndex < _auGridSteps.Length - 1)
        {
            _auGridStepIndex++;
            ApplyStepIndex();
        }
    }

    void ApplyStepIndex()
    {
        AUPerGrid = _auGridSteps[_auGridStepIndex];

        float pixelsPerCell = _mapGrid != null ? _mapGrid.PixelsPerCell : 80f;

        // Derive MapScale so that one grid cell = AUPerGrid AU = pixelsPerCell pixels
        MapScale = pixelsPerCell / (AUPerGrid * (float)PhysicsConstants.UNITY_UNITS_PER_AU);

        ClearAllTrails();
        UpdateGridLabel();
    }

    int FindClosestStepIndex(float au)
    {
        int best = 0;
        float bestDiff = Mathf.Abs(_auGridSteps[0] - au);

        for (int i = 1; i < _auGridSteps.Length; i++)
        {
            float diff = Mathf.Abs(_auGridSteps[i] - au);
            if (diff < bestDiff) { bestDiff = diff; best = i; }
        }

        return best;
    }

    void UpdateGridLabel()
    {
        string label = AUPerGrid < 1f
            ? $"{AUPerGrid:G2} AU"
            : $"{AUPerGrid:G6} AU";

        if (_auPerGridLabel != null) _auPerGridLabel.text = $"{AUPerGrid} AU / Grid";
        if (_horizontalGridScaleLabel != null) _horizontalGridScaleLabel.text = label;
        if (_verticalGridScaleLabel != null) _verticalGridScaleLabel.text = label;
    }

    public double3 GetRelativePosition(MapObjectManager objectManager) => objectManager.GetGlobalPosition() - _playerGlobalPosition;

    public void Initialize()
    {
        ClearMap();

        if (NBodyManager.Instance == null) return;
        if (NBodyManager.Instance.SystemBodies == null) return;

        foreach (AstronomicalObject astroObject in NBodyManager.Instance.SystemBodies)
        {
            if (astroObject == null) continue;

            string bodyName = astroObject.Data.Body.Name;
            if (_simulationMapObjects.ContainsKey(bodyName)) continue;

            CreateMapObject(astroObject);
        }
    }

    void ClearMap()
    {
        foreach (MapObjectManager mapObj in _mapObjectList)
        {
            if (mapObj != null)
            {
                mapObj.DestroyTrail();
                Destroy(mapObj.gameObject);
            }
        }

        _simulationMapObjects.Clear();
        _mapObjectList.Clear();
    }

    public void DestroyMapObject(string name)
    {
        if (!_simulationMapObjects.TryGetValue(name, out MapObjectManager mapObjectManager)) return;

        _simulationMapObjects.Remove(name);
        _mapObjectList.Remove(mapObjectManager);

        if (mapObjectManager != null)
        {
            mapObjectManager.DestroyTrail();
            Destroy(mapObjectManager.gameObject);
        }

        Debug.Log($"Destroyed map object for {name}");
    }

    public GameObject CreateMapObject(AstronomicalObject astroObject)
    {
        if (astroObject == null) return null;

        GameObject mapObjectGO = Instantiate(_mapObjectPrefabContainer, _mapParent);

        if (!mapObjectGO.TryGetComponent(out MapObjectManager mapObjectManager))
        {
            UIMessage.Instance.NewFadingMessage(MessageType.Error, $"Failed to create map object for {astroObject.Data.Body.Name}", 5f);
            Destroy(mapObjectGO);
            return null;
        }

        mapObjectManager.Initialize(astroObject);

        mapObjectManager.InitializeTrail(_mapParent, ProjectWorldToMap);

        string bodyName = astroObject.Data.Body.Name;

        if (_simulationMapObjects.ContainsKey(bodyName))
        {
            Destroy(mapObjectGO);
            return null;
        }

        _simulationMapObjects.Add(bodyName, mapObjectManager);
        _mapObjectList.Add(mapObjectManager);

        return mapObjectGO;
    }

    public void OnMapPlaneDropdownChanged(int index)
    {
        if (Enum.IsDefined(typeof(MapPlane), index))
            _currentMapPlane = (MapPlane)index;
        else
            Debug.LogWarning($"Invalid map plane index: {index}");
    }
}