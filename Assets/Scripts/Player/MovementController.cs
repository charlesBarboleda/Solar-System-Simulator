using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Mathematics;
using System.Collections.Generic;
using NaughtyAttributes;

public class MovementController : SimulationObject
{
    public static MovementController Instance { get; private set; }
    [SerializeField] Camera _playerCamera;
    public Camera PlayerCamera => _playerCamera;
    [SerializeField] float _customSpeedKmPerSec = 10f;

    [SerializeField] float _currentSpeedKmPerSec = 0f;
    float _accelerationKmPerSec2 = 1f;
    [SerializeField, Range(1.01f, 1.1f)] float _accelerationFactor = 1.01f;
    InputAction _moveAction;
    InputAction _verticalMoveAction;

    public float SpeedKmPerSec => _currentSpeedKmPerSec;
    const double MAX_SPEED_KM_PER_SEC = (PhysicsConstants.REAL_SPEED_OF_LIGHT_M_PER_S / 1000.0) * 3000; // Max speed is 3000x speed of light per second
    const float MIN_SPEED_KM_PER_SEC = 0f;

    [Header("Movement Safety")]
    [SerializeField] float _playerClearanceUnity = 0.25f;
    [SerializeField] float _slowdownLeadTimeSeconds = 2f;
    [SerializeField] float _minimumSlowdownDistanceUnity = 5f;
    [SerializeField] float _stopEpsilonUnity = 0.02f;
    [SerializeField] float _speedRecoveryRate = 0.5f;

    float _smoothedSafetyFactor = 1f;

    [SerializeField] bool _ignoreMovementSafetyForTesting = false;

    // Events
    public event Action<float, float> OnSpeedChanged;

    public bool IsTeleporting { get; private set; } = false;

    readonly Dictionary<AstronomicalObject, float> _bodySurfaceDistanceCache = new();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        IsTeleporting = false;

    }

    void Start()
    {
        _moveAction = InputSystem.actions.FindAction("Move");
        _verticalMoveAction = InputSystem.actions.FindAction("MoveUpDown");
        _moveAction?.Enable();
        _verticalMoveAction?.Enable();

        RenderSpace.SetAnchor(this);
        RenderSpace.SetOrigin(Position);
        UpdateTransform();
    }

    [Button]
    public void MoveObject()
    {
        AstronomicalObject astroObject = NBodyManager.Instance.SystemBodies[1];

        astroObject.SetPosition(new(astroObject.Position.x + 500f, astroObject.Position.y, astroObject.Position.z));
        Debug.Log($"[{astroObject.Data.Body.Name}] New Position: {astroObject.Position}");
    }

    [Button]
    public void CreateStarTest()
    {
        Data data = new();
        data.Body.Diameter = 999999999;
        data.Body.Mass = 9999999;
        data.Body.Name = "Test";
        data.Body.Type = BodyType.Star;
        data.Position.StartPosition = new(5000, 0, 0);
        data.Velocity.StartVelocity = new(0, 0, 0);
        data.Rotation.AxialTiltDeg = 1;
        data.Rotation.RotationPeriod = 2400;
        data.Rotation.IsBasicRotation = true;

        AstronomicalObjectFactory.Instance.CreateAstronomicalObject(data, AddToRuntime: true, AddToAssetDatabase: false);
    }

    void Update()
    {
        if (UIInputStopper.Instance.IsUIActive) return;
        if (IsTeleporting) return;

        Movement();
    }

    void Movement()
    {
        if (_moveAction == null || _verticalMoveAction == null) return;

        Vector2 moveValue = _moveAction.ReadValue<Vector2>();
        float verticalMoveValue = _verticalMoveAction.ReadValue<float>();

        // (Optimization) Calculate direction once
        Vector3 moveInput = transform.right * moveValue.x + transform.forward * moveValue.y + Vector3.up * verticalMoveValue;
        bool hasInput = moveInput.sqrMagnitude > 1e-4f;
        Vector3 moveDirection = moveInput.normalized;

        if (hasInput)
        {
            float targetSpeed;

            if (_customSpeedKmPerSec > 0f)
            {
                targetSpeed = Mathf.Clamp(_customSpeedKmPerSec, MIN_SPEED_KM_PER_SEC, (float)MAX_SPEED_KM_PER_SEC);
            }
            else
            {
                _accelerationKmPerSec2 *= _accelerationFactor;
                _currentSpeedKmPerSec += _accelerationKmPerSec2 * Time.deltaTime;
                targetSpeed = Mathf.Clamp(_currentSpeedKmPerSec, MIN_SPEED_KM_PER_SEC, (float)MAX_SPEED_KM_PER_SEC);
            }

            _currentSpeedKmPerSec = targetSpeed;
        }
        else
        {
            _accelerationKmPerSec2 = 1f;
            _currentSpeedKmPerSec = 0f;
        }

        float kmPerUnit = (float)(PhysicsConstants.UNITY_METERS_PER_UNIT / 1000.0);
        float speedUnitsPerSec = (kmPerUnit > 0f) ? (_currentSpeedKmPerSec / kmPerUnit) : 0f;

        float desiredMoveDistance = speedUnitsPerSec * Time.deltaTime;

        float rawFactor = CalculateMovementSafetyFactor(moveDirection, desiredMoveDistance, speedUnitsPerSec);

        if (rawFactor > _smoothedSafetyFactor) _smoothedSafetyFactor = Mathf.MoveTowards(_smoothedSafetyFactor, rawFactor, _speedRecoveryRate * Time.deltaTime);
        else _smoothedSafetyFactor = rawFactor;

        if (_ignoreMovementSafetyForTesting) _smoothedSafetyFactor = 1f;

        float finalMoveDistance = desiredMoveDistance * _smoothedSafetyFactor;
        _currentSpeedKmPerSec *= _smoothedSafetyFactor;

        Position += (double3)(float3)(moveDirection * finalMoveDistance);
    }


    public void SetSpeed(float newSpeedKmPerSec)
    {
        OnSpeedChanged?.Invoke(_customSpeedKmPerSec, newSpeedKmPerSec);
        _customSpeedKmPerSec = newSpeedKmPerSec;
    }

    public override double GetCollisionRadius(bool hasPadding = false) => PhysicsConstants.ToUnityUnitsFromKM(10000.0);

    float CalculateMovementSafetyFactor(Vector3 moveDirection, float desiredMoveDistance, float speedUnitsPerSecond)
    {
        if (desiredMoveDistance <= 0f) return 1f;

        if (NBodyManager.Instance == null) return 1f;

        Vector3 playerPos = (Vector3)(float3)GetGlobalPosition();

        Vector3 desiredEndPos = playerPos + moveDirection * desiredMoveDistance;

        float closestSurfaceDistance = float.MaxValue;
        float hardLimitFactor = 1f;

        var bodies = NBodyManager.Instance.SystemBodies;

        for (int i = 0; i < bodies.Count; i++)
        {
            AstronomicalObject body = bodies[i];
            if (body == null) continue;

            Vector3 bodyPos = (Vector3)(float3)body.GetGlobalPosition();

            float baseRadius = body.GetEffectiveSafetyRadiusUnity();

            float scaledClearance = _playerClearanceUnity + Mathf.Sqrt(baseRadius) * 0.35f;

            float bodyRadius = baseRadius + scaledClearance;

            Vector3 toBody = bodyPos - playerPos;
            float approachDot = Vector3.Dot(moveDirection, toBody.normalized);
            bool movingTowardBody = approachDot > 0f;

            float centerDistance = toBody.magnitude;

            if (centerDistance <= bodyRadius)
            {
                Vector3 awayVector = playerPos - bodyPos;

                if (awayVector.sqrMagnitude < 0.000001f) return 0f;

                Vector3 awayDirection = awayVector.normalized;

                float escapeDot = Vector3.Dot(moveDirection, awayDirection);

                if (escapeDot <= 0f) return 0f;

                continue;
            }

            float surfaceDistance = centerDistance - bodyRadius;

            // Cache for visual scaling regardless of movement direction
            if (body.NeedsVisualScaling) _bodySurfaceDistanceCache[body] = surfaceDistance;

            if (movingTowardBody && surfaceDistance < closestSurfaceDistance) closestSurfaceDistance = surfaceDistance;

            if (SegmentIntersectsSphere(playerPos, desiredEndPos, bodyPos, bodyRadius, out float hitT))
            {
                float allowedDistance = Mathf.Max(0f, (hitT * desiredMoveDistance) - _stopEpsilonUnity);

                float factor = allowedDistance / desiredMoveDistance;
                hardLimitFactor = Mathf.Min(hardLimitFactor, factor);
            }
        }

        float slowdownDistance = Mathf.Max(_minimumSlowdownDistanceUnity, speedUnitsPerSecond * _slowdownLeadTimeSeconds);
        float slowdownFactor = 1f;

        if (closestSurfaceDistance < slowdownDistance) slowdownFactor = Mathf.Clamp01(closestSurfaceDistance / slowdownDistance);

        foreach (var kvp in _bodySurfaceDistanceCache)
        {
            float perBodySlowdownFactor = Mathf.Clamp01(kvp.Value / slowdownDistance);
            kvp.Key.UpdateVisualScale(1f - perBodySlowdownFactor);
        }
        _bodySurfaceDistanceCache.Clear();

        return Mathf.Min(slowdownFactor, hardLimitFactor);
    }

    static bool SegmentIntersectsSphere(Vector3 start, Vector3 end, Vector3 center, float radius, out float tHit)
    {
        Vector3 direction = end - start;
        if (direction.sqrMagnitude < 0.0000001f)
        {
            tHit = 0f;
            return false;
        }

        Vector3 offset = start - center;

        float a = Vector3.Dot(direction, direction);
        float b = 2f * Vector3.Dot(offset, direction);
        float c = Vector3.Dot(offset, offset) - (radius * radius);

        float discriminant = (b * b) - (4f * a * c);

        if (discriminant < 0f)
        {
            tHit = 0f;
            return false;
        }

        float sqrtDisc = Mathf.Sqrt(discriminant);

        float t0 = (-b - sqrtDisc) / (2f * a);
        float t1 = (-b + sqrtDisc) / (2f * a);

        if (t0 >= 0f && t0 <= 1f)
        {
            tHit = t0;
            return true;
        }

        if (t1 >= 0f && t1 <= 1f)
        {
            tHit = t1;
            return true;
        }

        tHit = 0f;
        return false;
    }

    public override bool SetPositionNear(SimulationObject targetObject)
    {
        if (targetObject == null) return false;

        IsTeleporting = true;

        double3 targetPosition = targetObject.Position;
        double3 direction = Position - targetPosition;

        if (math.lengthsq(direction) <= 1e-12) direction = new double3(1, 0, 0);
        else direction = math.normalize(direction);

        double objectRadius = targetObject.GetCollisionRadius();
        float fovDeg = _playerCamera != null ? _playerCamera.fieldOfView : 60f;
        const float fillFraction = 0.55f;
        double halfAngleRad = (fovDeg * 0.5f * fillFraction) * Mathf.Deg2Rad;
        double idealViewDistance = objectRadius / Math.Tan(halfAngleRad);

        double safeDistance = NBodyManager.Instance.GetSafeDistanceBetweenObjects(this, targetObject) * 1.25;
        double finalDistance = Math.Max(idealViewDistance, safeDistance);

        double3 finalPosition = targetPosition + direction * finalDistance;

        if (NBodyManager.Instance.IsPositionOccupied(this, finalPosition, exclude: targetObject))
        {
            Debug.LogWarning("[Teleport] Position still occupied.");
            IsTeleporting = false;
            return false;
        }

        SetPosition(finalPosition, true);
        ForceUpdateProximityVisualScales(targetObject as AstronomicalObject);

        IsTeleporting = false;
        return true;
    }

    void ForceUpdateProximityVisualScales(AstronomicalObject primaryTarget = null)
    {
        if (NBodyManager.Instance == null) return;

        var bodies = NBodyManager.Instance.SystemBodies;
        if (bodies == null || bodies.Count == 0) return;

        Vector3 playerPos = (Vector3)(float3)GetGlobalPosition();

        for (int i = 0; i < bodies.Count; i++)
        {
            AstronomicalObject body = bodies[i];
            if (body == null || !body.NeedsVisualScaling) continue;

            if (ReferenceEquals(body, primaryTarget))
            {
                body.UpdateVisualScale(1f);
                continue;
            }

            Vector3 bodyPos = (Vector3)(float3)body.GetGlobalPosition();
            float baseRadius = body.GetEffectiveSafetyRadiusUnity();
            float bodyRadius = baseRadius + _playerClearanceUnity + Mathf.Sqrt(baseRadius) * 0.35f;
            float surfaceDistance = Mathf.Max(0f, Vector3.Distance(playerPos, bodyPos) - bodyRadius);

            float slowdownDistance = Mathf.Max(_minimumSlowdownDistanceUnity, bodyRadius * 2f);
            float t = 1f - Mathf.Clamp01(surfaceDistance / slowdownDistance);
            body.UpdateVisualScale(t);
        }
    }

    public Vector3 GetForwardDirection() => transform.forward;

    public void TeleportTo(AstronomicalObject astroObject)
    {
        if (!SetPositionNear(astroObject)) return;

        double3 toTarget = astroObject.Position - Position;
        if (math.lengthsq(toTarget) < 1e-12) return;

        Vector3 lookDir = (Vector3)(float3)math.normalize(toTarget);
        LookController.Instance.SetLookDirection(lookDir);
    }

    [Button]
    public void TeleportLookTest1()
    {
        AstronomicalObject astroObject = NBodyManager.Instance.SystemBodies[0];

        TeleportTo(astroObject);
    }

    [Button]
    public void TeleportLookTest2()
    {
        AstronomicalObject astroObject = NBodyManager.Instance.SystemBodies[1];

        TeleportTo(astroObject);
    }

    [Button]
    public void TeleportTest1()
    {
        AstronomicalObject astroObject = NBodyManager.Instance.SystemBodies[0];

        SetPositionNear(astroObject);
    }

    [Button]
    public void TeleportTest2()
    {
        AstronomicalObject astroObject = NBodyManager.Instance.SystemBodies[1];

        SetPositionNear(astroObject);
    }

}
