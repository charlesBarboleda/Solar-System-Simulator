using UnityEngine;
using Unity.Mathematics;
using NaughtyAttributes;
using JetBrains.Annotations;


public class AstronomicalObject : SimulationObject
{
    public Data Data;

    [Header("Light Source")]
    [SerializeField] LightManager _lightManagerObject;

    [Header("Rotation Hierarchy")]
    [Tooltip("Transform that handles the body's axis orientation (tilt).")]
    [SerializeField] Transform _axisRoot;

    [Tooltip("Transform that spins around the local Y axis after the axis has been oriented.")]
    [SerializeField] Transform _spinRoot;

    [Tooltip("Optional visual-only child transform for mesh/material corrections.")]
    [SerializeField] Transform _visualRoot;

    [Header("Rotation Debug")]
    [SerializeField, ReadOnly] double _currentSpinAngleRad;
    [SerializeField, ReadOnly] double _angularSpeedRadPerSimSecond;
    [SerializeField, ReadOnly] Vector3 _debugPoleDirection;

    [Header("Proximity Radius")]
    [SerializeField, ReadOnly] float _cachedSafetyRadiusUnity;

    [Header("Visual Proximity Scaling")]
    [SerializeField] float _visualScaleMaxComponent = 3f;
    [SerializeField, ReadOnly] float _simulationScaleComponent;
    [SerializeField, ReadOnly] bool _needsVisualScaling;
    public bool NeedsVisualScaling => _needsVisualScaling;

    [SerializeField] bool _initialized = false;
    // Cached variables
    float _boundsOffsetKM;


    [Button]
    void InitializeTest()
    {
        Data data = new();

        data.Body.Name = "SunTest";
        data.Body.Diameter = 99999999;
        data.Body.Mass = 9999999;
        data.Body.Type = BodyType.Star;
        data.Body.Temperature = 5800;

        data.Position.StartPosition = double3.zero;
        data.Velocity.StartVelocity = double3.zero;

        Initialize(data);
    }

    public void Initialize(Data data)
    {
        Data = data;

        if (Data.Body.Mass <= 0)
        {
            Debug.LogError($"[AstronomicalObject.cs] {Data.Body.Name}: Data.Body.Mass invalid.");
            return;
        }

        if (!_initialized)
        {
            InitializeVelocity(Data);
            InitializePosition(Data);
            InitializeRotation(Data);
            InitializeSize(Data);

            if (data.Body.Type == BodyType.Star)
            {
                InitializeStar(Data);
            }

            if (data.Ring.IsRingPlanet && data.Body.Type == BodyType.Planet)
            {
                if (TryGetComponent(out RingPlanet ringPlanet))
                {
                    ringPlanet.SetProperties((float)data.Ring.InnerGapKM, (float)data.Ring.RingWidthKM, (float)data.Body.Diameter);
                    ringPlanet.Initialize();
                }
                else Debug.LogError($"[{Data.Body.Name}] Ring planet data provided but no RingPlanet component found.");
            }
        }

        UpdateTransform();

        _initialized = true;
    }

    public override double GetCollisionRadius()
    {
        double radiusMeters = (Data.Body.Diameter * 0.5) * 1.25;

        return PhysicsConstants.ToUnityUnitsFromM(radiusMeters);
    }

    void InitializeVelocity(Data data) => Velocity = data.Velocity.StartVelocity;
    void InitializePosition(Data data) => Position = data.Position.StartPosition;

    void InitializeSize(Data data)
    {
        float simulationDiameter = (float)PhysicsConstants.ToUnityUnitsFromM(data.Body.Diameter);
        float sizeScaleFactor = (float)PhysicsConstants.UNITY_SIZE_SCALE_FACTOR;

        CalculateBoundsOffsetKM();

        if (simulationDiameter > 0)
        {
            if (data.Body.Type == BodyType.Planet && data.Ring.IsRingPlanet)
            {
                TryGetComponent(out RingPlanet ringPlanet);

                ringPlanet.UnparentRing();

                _simulationScaleComponent = simulationDiameter * sizeScaleFactor;
                transform.localScale = Vector3.one * _simulationScaleComponent;

                _needsVisualScaling = transform.localScale.x < _visualScaleMaxComponent;

                ringPlanet.ParentRing();
            }
            else
            {
                _simulationScaleComponent = simulationDiameter * sizeScaleFactor;
                transform.localScale = Vector3.one * _simulationScaleComponent;

                _needsVisualScaling = transform.localScale.x < _visualScaleMaxComponent;
            }

        }
        else Debug.LogWarning($"Diameter is too small or invalid for {data.Body.Name}. Check Data.Diameter.");
    }

    void InitializeStar(Data data)
    {
        if (data.Body.Type != BodyType.Star) return;

        if (!TryGetComponent(out SunRenderingManager sunRenderingManager))
        {
            Debug.LogWarning($"No 'SunRenderingManager' script attached to this object.");
            return;
        }
        else sunRenderingManager.Initialize(data);
    }


    void InitializeRotation(Data data)
    {
        if (!HasRequiredRotationReferences()) return;

        if (data.Rotation.IsBasicRotation) InitializeRotationBasic(data);
        else InitializeRotationAdvanced(data);
    }

    /// Basic mode: only RotationPeriod and AxialTiltDeg are required.
    void InitializeRotationBasic(Data data)
    {
        if (data.Rotation.RotationPeriod <= 0.0)
        {
            Debug.LogWarning($"[{data.Body.Name}] Basic rotation is enabled but RotationPeriod is 0 or negative. Rotation will not be initialized.");
            return;
        }

        // Convert period (hours) → angular speed (rad / sim-second)
        _angularSpeedRadPerSimSecond = (2.0 * math.PI) / (data.Rotation.RotationPeriod * 3600.0);

        if (data.Rotation.Retrograde) _angularSpeedRadPerSimSecond *= -1.0f;

        // Only axial tilt matters in basic mode; azimuth defaults to 0
        Vector3 poleDirection = GetPoleDirectionFromTilt((float)data.Rotation.AxialTiltDeg, azimuthDeg: 0f);
        _debugPoleDirection = poleDirection;
        _axisRoot.rotation = Quaternion.FromToRotation(Vector3.up, poleDirection.normalized);

        _currentSpinAngleRad = 0.0;
        ApplySpinRotation();
    }

    /// Advanced mode: requires full rotation input configuration
    void InitializeRotationAdvanced(Data data)
    {
        bool hasRate = math.abs(data.Rotation.RotationRate) > 0.0;
        bool hasSidereal = data.Rotation.MeanSiderealDay > 0.0;

        if (!hasRate && !hasSidereal)
        {
            Debug.LogWarning($"[{data.Body.Name}] Advanced rotation: neither RotationRate nor MeanSiderealDay is set. Rotation will not be initialized.");
            return;
        }

        if (hasRate && hasSidereal)
        {
            Debug.LogWarning($"[{data.Body.Name}] Advanced rotation: both RotationRate and MeanSiderealDay are set. Only one may be used.");
            return;
        }

        _angularSpeedRadPerSimSecond = hasRate ? math.abs(data.Rotation.RotationRate) : (2.0 * math.PI) / (data.Rotation.MeanSiderealDay * 3600.0);

        if (data.Rotation.Retrograde) _angularSpeedRadPerSimSecond *= -1.0f;

        Vector3 poleDirection = GetPoleDirectionFromTilt((float)data.Rotation.AxialTiltDeg, (float)data.Rotation.AxisAzimuthDeg);

        _debugPoleDirection = poleDirection;
        _axisRoot.rotation = Quaternion.FromToRotation(Vector3.up, poleDirection.normalized);

        double initialSpinDeg = data.Rotation.InitialSpinDeg + data.Rotation.ModelPrimeMeridianOffset;
        _currentSpinAngleRad = math.radians(initialSpinDeg);

        ApplySpinRotation();
    }


    bool HasRequiredRotationReferences()
    {
        if (_axisRoot == null)
        {
            Debug.LogWarning($"[{Data.Body.Name}] Missing AxisRoot reference. Assigning this object as AxisRoot.");
            _axisRoot = gameObject.transform;
        }

        if (_spinRoot == null)
        {
            Debug.LogWarning($"[{Data.Body.Name}] Missing SpinRoot reference. Assigning this object as SpinRoot.");
            _spinRoot = gameObject.transform;
        }

        return true;
    }

    Vector3 GetPoleDirectionFromTilt(float axialTiltDeg, float azimuthDeg)
    {
        Vector3 simulationUp = Vector3.up;
        Vector3 tiltDirection = Quaternion.AngleAxis(azimuthDeg, simulationUp) * Vector3.forward;
        Vector3 tiltAxis = Vector3.Cross(simulationUp, tiltDirection).normalized;

        if (tiltAxis.sqrMagnitude <= 0.000001f) return simulationUp;

        return Quaternion.AngleAxis(axialTiltDeg, tiltAxis) * simulationUp;
    }

    void ApplySpinRotation()
    {
        if (_spinRoot == null) return;

        float spinDeg = (float)math.degrees(_currentSpinAngleRad);
        _spinRoot.localRotation = Quaternion.AngleAxis(spinDeg, Vector3.up);
    }

    double WrapAngleRad(double angleRad)
    {
        angleRad = math.fmod(angleRad, math.PI * 2.0);
        if (angleRad < 0.0) angleRad += math.PI * 2.0;
        return angleRad;
    }

    public void StepRotation(double simulatedSeconds)
    {
        if (_spinRoot == null) return;
        if (math.abs(_angularSpeedRadPerSimSecond) <= 0.0) return;

        _currentSpinAngleRad += _angularSpeedRadPerSimSecond * simulatedSeconds;
        _currentSpinAngleRad = math.fmod(_currentSpinAngleRad, math.PI * 2.0);
        if (_currentSpinAngleRad < 0.0) _currentSpinAngleRad += math.PI * 2.0;

        ApplySpinRotation();
    }

    public void UpdateVisualScale(float t)
    {
        if (!_needsVisualScaling) return;

        float visualScale = Mathf.Lerp(_simulationScaleComponent, _visualScaleMaxComponent, t);
        transform.localScale = Vector3.one * visualScale;
    }

    public void DestroyFarVisuals()
    {
        if (Data.Body.Type != BodyType.Star) return;

        if (!TryGetComponent(out SunRenderingManager sunRenderingManager))
        {
            Debug.LogWarning($"No 'SunRenderingManager' script attached to this object.");
            return;
        }
        else sunRenderingManager.DestroyFarVisuals();
    }

    public void DestroyLightSource()
    {
        if (Data.Body.Type != BodyType.Star) return;

        if (_lightManagerObject == null)
        {
            Debug.Log($"No light source reference found on this star object.");
            return;
        }

        Destroy(_lightManagerObject.gameObject);
    }

    public void SetLightSource(LightManager lightManager)
    {
        if (lightManager == null)
        {
            Debug.LogError($"Could not set a light source for {Data.Body.Name}, light source is null");
            return;
        }

        _lightManagerObject = lightManager;
    }


    public float GetBoundsOffsetKM() => _boundsOffsetKM;
    public float GetSafetyRadiusUnity() => _cachedSafetyRadiusUnity;

    public float GetEffectiveSafetyRadiusUnity()
    {
        if (!_needsVisualScaling) return _cachedSafetyRadiusUnity;

        float visualMaxRadius = _visualScaleMaxComponent * 0.5f;
        return Mathf.Max(_cachedSafetyRadiusUnity, visualMaxRadius);
    }

    public void CalculateBoundsOffsetKM()
    {
        float radiusMeters = (float)(Data.Body.Diameter * 0.5);

        _boundsOffsetKM = radiusMeters / 1000f;

        _cachedSafetyRadiusUnity = (float)PhysicsConstants.ToUnityUnitsFromM(radiusMeters);

    }

    public void AdvanceRotationBySimDays(double dtDays)
    {
        if (dtDays <= 0.0) return;
        AdvanceRotationBySimSeconds(dtDays * PhysicsConstants.REAL_SECONDS_PER_DAY);
    }

    public void AdvanceRotationBySimSeconds(double dtSeconds)
    {
        if (_spinRoot == null) return;
        if (dtSeconds <= 0.0) return;
        if (math.abs(_angularSpeedRadPerSimSecond) <= 0.0) return;

        _currentSpinAngleRad += _angularSpeedRadPerSimSecond * dtSeconds;
        _currentSpinAngleRad = WrapAngleRad(_currentSpinAngleRad);

        ApplySpinRotation();
    }
}
